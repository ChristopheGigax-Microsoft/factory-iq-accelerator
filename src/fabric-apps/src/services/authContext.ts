export type UserContext = {
  objectId?: string;
  userPrincipalName?: string;
  displayName?: string;
};

export function resolveActor(user: UserContext | null): string {
  if (!user) {
    return 'unknown-user';
  }
  return user.userPrincipalName ?? user.objectId ?? user.displayName ?? 'unknown-user';
}
