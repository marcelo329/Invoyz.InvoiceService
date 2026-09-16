/**
 * Hands a blob to the browser as a file download.
 *
 * Isolated from anything that fetches, so the network code stays free of DOM concerns
 * and this stays the one place the object URL is released — forgetting the revoke
 * leaks the whole file until the tab is closed.
 */
export function downloadBlob(blob: Blob, fileName: string): void {
  const objectUrl = URL.createObjectURL(blob)

  try {
    const anchor = document.createElement('a')

    anchor.href = objectUrl
    anchor.download = fileName
    anchor.rel = 'noopener'
    anchor.style.display = 'none'

    document.body.appendChild(anchor)
    anchor.click()
    anchor.remove()
  } finally {
    // Deferred: revoking immediately can cancel the download in some browsers before
    // they have finished reading from the URL.
    setTimeout(() => URL.revokeObjectURL(objectUrl), 10_000)
  }
}
