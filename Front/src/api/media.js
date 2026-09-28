export function resolveImageUrl(imageUrl, apiBase) {
  if (!imageUrl) return null
  if (imageUrl.startsWith('http://') || imageUrl.startsWith('https://')) {
    return imageUrl
  }
  return `${apiBase}${imageUrl}`
}
