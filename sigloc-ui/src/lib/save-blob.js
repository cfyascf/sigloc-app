// Authenticated downloads (e.g. trip proofs) are fetched as a Blob and saved through a temporary link.
export function saveBlob(blob, fileName) {
  const url = URL.createObjectURL(blob)
  const link = document.createElement("a")
  link.href = url
  link.download = fileName
  document.body.appendChild(link)
  link.click()
  link.remove()
  setTimeout(() => URL.revokeObjectURL(url), 0)
}
