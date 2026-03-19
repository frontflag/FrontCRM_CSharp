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

export interface RemoveTagsRequest {
  entityType: string;
  entityIds: string[];
  tagIds: string[];
}

export const tagApi = {
  /** 获取所有标签定义（可按实体类型过滤） */
  async getTagDefinitions(entityType?: string): Promise<TagDefinitionDto[]> {
    try {
      const url = entityType
        ? `/api/v1/tags?entityType=${encodeURIComponent(entityType)}&pageNumber=1&pageSize=500`
        : `/api/v1/tags?pageNumber=1&pageSize=500`;
      const res = await apiClient.get<any>(url);
      const payload = res && typeof res === 'object' && 'data' in res ? res.data : res;
      if (payload && typeof payload === 'object') {
        // 后端返回分页结构 { items, totalCount, ... }
        if ('items' in payload && Array.isArray((payload as any).items)) {
          return (payload as any).items as TagDefinitionDto[];
        }
        if (Array.isArray((payload as any).data)) {
          return (payload as any).data as TagDefinitionDto[];
        }
      }
      if (Array.isArray(payload)) return payload as TagDefinitionDto[];
      return [];
    } catch {
      return [];
    }
  },

  /** 获取实体已打的标签 */
  async getEntityTags(entityType: string, entityId: string): Promise<TagDefinitionDto[]> {
    try {
      const res = await apiClient.get<any>(`/api/v1/tags/entities/${encodeURIComponent(entityType)}/${encodeURIComponent(entityId)}`);
      const payload = res && typeof res === 'object' && 'data' in res ? res.data : res;
      if (Array.isArray(payload)) return payload as TagDefinitionDto[];
      return [];
    } catch {
      return [];
    }
  },

  /** 为实体打标签 */
  async applyTags(request: ApplyTagsRequest): Promise<void> {
    // 让调用方感知并展示真实错误信息
    await apiClient.post('/api/v1/tags/apply', request);
  },

  /** 批量移除标签 */
  async removeTags(request: RemoveTagsRequest): Promise<void> {
    await apiClient.post('/api/v1/tags/remove', request);
  },

  /** 移除实体的某个标签（语法糖） */
  async removeTag(entityType: string, entityId: string, tagId: string): Promise<void> {
    await apiClient.post('/api/v1/tags/remove', {
      entityType,
      entityIds: [entityId],
      tagIds: [tagId],
    } as RemoveTagsRequest);
  },

  /** 创建新标签定义 */
  async createTagDefinition(data: { name: string; color?: string; entityType?: string; description?: string }): Promise<TagDefinitionDto | null> {
    try {
      const res = await apiClient.post<any>('/api/v1/tags', data);
      const payload = res && typeof res === 'object' && 'data' in res ? res.data : res;
      return payload as TagDefinitionDto;
    } catch {
      return null;
    }
  },
};
