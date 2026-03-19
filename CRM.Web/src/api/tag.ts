import apiClient from './client';

export interface TagDefinitionDto {
  id: string;
  name: string;
  color?: string;
  entityType?: string;
  description?: string;
}

export interface ApplyTagsRequest {
  entityType: string;
  entityIds: string[];
  tagIds: string[];
}

export const tagApi = {
  /** 获取所有标签定义（可按实体类型过滤） */
  async getTagDefinitions(entityType?: string): Promise<TagDefinitionDto[]> {
    try {
      const url = entityType
        ? `/api/v1/tags?entityType=${entityType}`
        : `/api/v1/tags`;
      const res = await apiClient.get<any>(url);
      if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
        return res.data as TagDefinitionDto[];
      if (Array.isArray(res)) return res;
      return [];
    } catch {
      return [];
    }
  },

  /** 获取实体已打的标签 */
  async getEntityTags(entityType: string, entityId: string): Promise<TagDefinitionDto[]> {
    try {
      const res = await apiClient.get<any>(`/api/v1/tags/entity/${entityType}/${entityId}`);
      if (res && typeof res === 'object' && 'data' in res && Array.isArray(res.data))
        return res.data as TagDefinitionDto[];
      if (Array.isArray(res)) return res;
      return [];
    } catch {
      return [];
    }
  },

  /** 为实体打标签 */
  async applyTags(request: ApplyTagsRequest): Promise<void> {
    try {
      await apiClient.post('/api/v1/tags/apply', request);
    } catch {
      // 后端未实现时静默失败
    }
  },

  /** 移除实体的某个标签 */
  async removeTag(entityType: string, entityId: string, tagId: string): Promise<void> {
    try {
      await apiClient.delete(`/api/v1/tags/entity/${entityType}/${entityId}/${tagId}`);
    } catch {
      // 静默失败
    }
  },

  /** 创建新标签定义 */
  async createTagDefinition(data: { name: string; color?: string; entityType?: string; description?: string }): Promise<TagDefinitionDto | null> {
    try {
      const res = await apiClient.post<any>('/api/v1/tags', data);
      if (res && typeof res === 'object' && 'data' in res && res.data)
        return res.data as TagDefinitionDto;
      return res as TagDefinitionDto;
    } catch {
      return null;
    }
  },
};
