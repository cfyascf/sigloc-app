-- Production data clean-up after the QA passes of 2026-10-23 and 2026-10-27.
--
-- Run it BEFORE deploying the auto-award worker: otherwise the worker closes the stale
-- auto-award auctions itself and sends "leilão encerrado" notifications for old data.
--
-- How to use:
--   1. Run each "DRY RUN" SELECT and check the rows.
--   2. Run the whole file. Every section ends in ROLLBACK; change it to COMMIT once the
--      counts printed by the DRY RUN match what you expect.
-- Statuses are stored as text ('Open', 'Closed', 'InTransit', ...).


-- =====================================================================================
-- 1. Auctions left "Open" by the old award/cancel bug
--    (status change was lost, e.g. 722a63fd whose route is already COMPLETED)
--    An auction is stale when its route already has a trip / moved past the auction stage,
--    when it has a winning bid, or when its route has no segments left.
-- =====================================================================================

-- DRY RUN
SELECT a."Id", a."ExpiresAt", a."AutomaticAward", r."Status" AS route_status,
       EXISTS (SELECT 1 FROM "Bid" b WHERE b."AuctionId" = a."Id" AND b."Status" = 'Winner') AS has_winner,
       EXISTS (SELECT 1 FROM "Trip" t WHERE t."AuctionId" = a."Id" OR t."RouteId" = a."RouteId") AS has_trip,
       NOT EXISTS (SELECT 1 FROM "RouteSegment" s WHERE s."RouteId" = a."RouteId") AS no_segments
FROM "Auction" a
JOIN "ConsolidatedRoute" r ON r."Id" = a."RouteId"
WHERE a."Status" = 'Open'
  AND (r."Status" IN ('AwaitingPickup', 'InTransit', 'Completed')
       OR EXISTS (SELECT 1 FROM "Bid" b WHERE b."AuctionId" = a."Id" AND b."Status" = 'Winner')
       OR EXISTS (SELECT 1 FROM "Trip" t WHERE t."AuctionId" = a."Id" OR t."RouteId" = a."RouteId")
       OR NOT EXISTS (SELECT 1 FROM "RouteSegment" s WHERE s."RouteId" = a."RouteId"));

BEGIN;

-- Bids still "Pending" on an auction that already has a winner lost it.
UPDATE "Bid" b
SET "Status" = 'Losing'
WHERE b."Status" = 'Pending'
  AND EXISTS (SELECT 1 FROM "Bid" w WHERE w."AuctionId" = b."AuctionId" AND w."Status" = 'Winner')
  AND EXISTS (SELECT 1 FROM "Auction" a WHERE a."Id" = b."AuctionId" AND a."Status" = 'Open');

UPDATE "Auction" a
SET "Status" = 'Closed'
FROM "ConsolidatedRoute" r
WHERE r."Id" = a."RouteId"
  AND a."Status" = 'Open'
  AND (r."Status" IN ('AwaitingPickup', 'InTransit', 'Completed')
       OR EXISTS (SELECT 1 FROM "Bid" b WHERE b."AuctionId" = a."Id" AND b."Status" = 'Winner')
       OR EXISTS (SELECT 1 FROM "Trip" t WHERE t."AuctionId" = a."Id" OR t."RouteId" = a."RouteId")
       OR NOT EXISTS (SELECT 1 FROM "RouteSegment" s WHERE s."RouteId" = a."RouteId"));

ROLLBACK; -- change to COMMIT


-- =====================================================================================
-- 2. QA test products in the "Contratante" catalogue (created 2026-10-27)
--    QA-R2-001 (renamed "QA editado QA-R2-001"), par-001 (case duplicate of PAR-001),
--    and the 50-character "QA-SSSS..." SKU. None is used by a segment.
-- =====================================================================================

-- DRY RUN: expect 3 rows, linked_segments = 0
SELECT p."Id", p."Sku", p."Name",
       (SELECT count(*) FROM "ProductRouteSegment" x WHERE x."ProductId" = p."Id") AS linked_segments
FROM "Product" p
WHERE p."Id" IN ('01a17edd-9555-7822-ae33-a4a2b86f564d',
                 '01a17edd-5013-7b18-868d-0410f6049137',
                 '01a17edd-7e57-75d9-8927-ba7cf7d83918');

BEGIN;

DELETE FROM "Product" p
WHERE p."Id" IN ('01a17edd-9555-7822-ae33-a4a2b86f564d',
                 '01a17edd-5013-7b18-868d-0410f6049137',
                 '01a17edd-7e57-75d9-8927-ba7cf7d83918')
  AND NOT EXISTS (SELECT 1 FROM "ProductRouteSegment" x WHERE x."ProductId" = p."Id");

ROLLBACK; -- change to COMMIT


-- =====================================================================================
-- 3. Throwaway QA contractor account (created 2026-10-27 to test registration)
--    e-mail qa-dupcnpj-1793131956466@example.com, CNPJ 31.953.385/0001-35,
--    company a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb. It owns: 1 product (QA-P1), 2 segments,
--    1 undone auction and an ended partnership with "Transportadora". It never had a trip.
--    Children are deleted first: most company columns have no foreign key.
-- =====================================================================================

-- DRY RUN: what will be removed (trips must be 0, the delete stops otherwise)
SELECT 'contractor' AS what, count(*) FROM "Contractor" WHERE "Id" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'users', count(*) FROM "User" WHERE "CompanyId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'products', count(*) FROM "Product" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'segments', count(*) FROM "RouteSegment" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'routes', count(*) FROM "ConsolidatedRoute" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'trips', count(*) FROM "Trip" t JOIN "ConsolidatedRoute" r ON r."Id" = t."RouteId"
          WHERE r."ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'partnerships', count(*) FROM "PartnerConnection" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb'
UNION ALL SELECT 'notifications', count(*) FROM "Notification" WHERE "RecipientCompanyId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';

BEGIN;

DELETE FROM "Notification" WHERE "RecipientCompanyId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
DELETE FROM "PartnerConnection" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
DELETE FROM "PartnershipInvite" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
-- Segments first: their product links cascade, which frees the products.
DELETE FROM "RouteSegment" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
-- Routes cascade to their auctions, bids and blocked-bid attempts (fails if a trip exists).
DELETE FROM "ConsolidatedRoute" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
DELETE FROM "Product" WHERE "ContractorId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
DELETE FROM "PasswordResetToken" WHERE "UserId" IN
    (SELECT "Id" FROM "User" WHERE "CompanyId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb');
DELETE FROM "User" WHERE "CompanyId" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';
DELETE FROM "Contractor" WHERE "Id" = 'a57a97d9-e8fb-4e0d-ab3d-185ee24f24cb';

ROLLBACK; -- change to COMMIT
