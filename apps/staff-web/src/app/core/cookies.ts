/** Returns the decoded value of the named cookie, or an empty string when it is absent. */
export function readCookie(name: string, cookies: string = document.cookie): string {
  const prefix = `${name}=`;
  for (const part of cookies.split(';')) {
    const cookie = part.trimStart();
    if (cookie.startsWith(prefix)) {
      return decodeURIComponent(cookie.substring(prefix.length));
    }
  }
  return '';
}
