import apiClient from './client';

export interface FavoriteToggleRequest {
  entityType: string;
  entityId: string;
}

export const favoriteApi = {
  async addFavorite(payload: FavoriteToggleRequest): Promise<void> {
    await apiClient.post('/api/v1/favorites', payload);
  },

  async removeFavorite(entityType: string, entityId: string): Promise<void> {
    await apiClient.delete('/api/v1/favorites', {
      params: { entityType, entityId }
    });
  },

  async getFavoriteEntityIds(entityType: string): Promise<string[]> {
    const res = await apiClient.get<any>('/api/v1/favorites', {
      params: { entityType }
    });
    const payload = res && typeof res === 'object' && 'data' in res ? res.data : res;
    return Array.isArray(payload?.entityIds) ? payload.entityIds : [];
  },

  async checkFavorite(entityType: string, entityId: string): Promise<boolean> {
    const payload = await apiClient.get<{ isFavorite?: boolean }>('/api/v1/favorites/check', {
      params: { entityType, entityId }
    });
    return Boolean(payload?.isFavorite);
  }
};

export default favoriteApi;
