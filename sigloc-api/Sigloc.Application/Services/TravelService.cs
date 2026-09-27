using Sigloc.Application.DTOs;
using Sigloc.Application.Services.Interfaces;
using Sigloc.Domain.Entities;
using Sigloc.Domain.Repositories;

namespace Sigloc.Application.Services;

public class TravelService : ITravelService
{
    private readonly ITravelRepository _travelRepository;
    private readonly ITelemetryRepository _telemetryRepository;
    // Serviços externos (Traccar e OSRM) conectardos depois
    // private readonly ITrackingService _trackingService; 
    // private readonly IRoutingService _routingService;

    public TravelService(
        ITravelRepository travelRepository,
        ITelemetryRepository telemetryRepository)
    {
        _travelRepository = travelRepository;
        _telemetryRepository = telemetryRepository;
    }

    // Endpoint 1: Listagem Macro (sem APIs externas)
    public async Task<IEnumerable<TravelMacroResponseDto>> ObterViagensAtivasAsync()
    {
        var viagens = await _travelRepository.GetAllActiveAsync();

        return viagens.Select(v => new TravelMacroResponseDto(
            ViagemId: v.Id,
            StatusGeral: v.Status,
            ProgressoPercentual: v.Monitoramento?.UltimoProgressoPorcentual ?? 0,
            UltimoEtaCalculado: v.Monitoramento?.UltimoEtaCalculado,
            UltimaAtualizacaoPing: v.Monitoramento?.UltimaAtualizacaoPing
        ));
    }

    // Endpoint 2: O Motor On-Demand (Gatilho da inteligência geoespacial)
    public async Task<TravelDetalheResponseDto> ProcessarDetalhesViagemAsync(Guid travelId)
    {
        var viagem = await _travelRepository.GetByIdWithMonitoringAsync(travelId);
        if (viagem == null) throw new Exception("Viagem não encontrada.");

        var monitoramento = viagem.Monitoramento;
        var agora = DateTimeOffset.UtcNow;
        
        // Regra de Negócio: Checagem de Validade (Cache de 15 minutos)
        bool cacheExpirado = monitoramento.UltimaAtualizacaoPing == null || 
                             (agora - monitoramento.UltimaAtualizacaoPing.Value).TotalMinutes >= 15;

        double latAtual = 0;
        double lngAtual = 0;

        if (cacheExpirado)
        {
            // Cenário B: Cache Expirado. Bater no Traccar e OSRM.
            
            // 1. Simulação: Buscar nova coordenada no Traccar
            // var posicao = await _trackingService.GetLastPositionAsync(viagem.DeviceId);
            latAtual = -25.4284; // Mock
            lngAtual = -49.2733; // Mock

            // 2. Criar a "Caixa Preta" e salvar na tabela de Telemetria
            var novaTelemetria = new Telemetry(viagem.Id, latAtual, lngAtual);
            await _telemetryRepository.AddAsync(novaTelemetria);

            // 3. Simulação: Calcular novo ETA e Progresso no OpenRouteService
            // var rota = await _routingService.GetRouteMatrixAsync(...);
            decimal novoProgressoCalculado = 65.5m; // Mock pós-matemática OSRM
            DateTimeOffset novoEtaCalculado = agora.AddHours(2); // Mock
            
            // 4. Atualizar o Cache no Monitoramento
            monitoramento.AtualizarCache(novoProgressoCalculado, novoEtaCalculado);
            
            // (Opcional) Aqui entraria a validação if(novoEtaCalculado > deadline) para mudar o status da Viagem

            // 5. Salvar alterações no banco
            await _travelRepository.UpdateAsync(viagem);
        }

        // Devolve o DTO formatado para o front-end renderizar a Timeline
        return new TravelDetalheResponseDto(
            ViagemId: viagem.Id,
            StatusGeral: viagem.Status,
            ProgressoPercentual: monitoramento.UltimoProgressoPorcentual,
            NovoEta: monitoramento.UltimoEtaCalculado,
            IsCacheRenovado: cacheExpirado,
            UltimaLatitude: cacheExpirado ? latAtual : null,
            UltimaLongitude: cacheExpirado ? lngAtual : null
        );
    }
}