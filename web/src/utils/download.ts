// Saving or showing a file that was fetched as a blob (the API needs the bearer token, so a plain
// link or <img src> would 401).

/** Triggers a browser download of the blob under the given file name. */
export function saveBlob(blob: Blob, fileName: string): void {
  const url = URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  a.rel = 'noopener'
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  setTimeout(() => URL.revokeObjectURL(url), 10_000)
}

export interface BlobTab {
  /** Points the tab at the file, or downloads it if the tab was blocked or closed. */
  show: (blob: Blob, fallbackName: string) => void
  /** Closes the placeholder tab (the fetch failed). */
  cancel: () => void
}

/**
 * Opens a blank tab right away (while the click is still a user gesture, so pop-up blockers stay
 * quiet) and fills it once the bytes arrive.
 */
export function openBlobTab(title?: string): BlobTab {
  const tab = window.open('', '_blank')
  // Names the tab while the file loads; once a PDF is showing the browser's own viewer sets the title.
  if (tab && title) { try { tab.document.title = title } catch { /* cross-origin or blocked: ignore */ } }
  return {
    show(blob, fallbackName) {
      if (!tab || tab.closed) { saveBlob(blob, fallbackName); return }
      const url = URL.createObjectURL(blob)
      tab.location.href = url
      setTimeout(() => URL.revokeObjectURL(url), 60_000)
    },
    cancel() { if (tab && !tab.closed) tab.close() }
  }
}
