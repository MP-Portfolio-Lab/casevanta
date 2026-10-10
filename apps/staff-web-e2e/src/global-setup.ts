import { identityProvider } from './support/identity-provider';

/** The realm import provides the users; the journeys only need the identity provider to be ready. */
export default async function globalSetup(): Promise<void> {
  await identityProvider.waitUntilReady();
}
