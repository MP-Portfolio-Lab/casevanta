import { readCookie } from './cookies';

describe('readCookie', () => {
  it('returns the decoded value of the named cookie', () => {
    expect(readCookie('XSRF-RequestToken', 'a=1; XSRF-RequestToken=abc%2Bdef; b=2')).toBe(
      'abc+def',
    );
  });

  it('returns an empty string when the cookie is absent', () => {
    expect(readCookie('XSRF-RequestToken', 'a=1; b=2')).toBe('');
  });

  it('does not match a cookie whose name only ends with the requested name', () => {
    expect(readCookie('Token', 'XSRF-Token=1')).toBe('');
  });
});
