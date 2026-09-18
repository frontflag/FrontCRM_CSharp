import apiClient from './client'

export interface WorkCalendarDayCount {
  date: string
  rfqCount: number
  soCount: number
  taskCount: number
}

export interface WorkCalendarMonth {
  year: number
  month: number
  days: WorkCalendarDayCount[]
}

export interface WorkCalendarDocItem {
  id: string
  code: string
  customerName?: string | null
}

export interface WorkTaskListItem {
  id: string
  title: string
  status: number
  priority: number
  startDate: string
  customerName?: string | null
  objectId: string
}

export interface WorkCalendarDay {
  date: string
  rfqs: WorkCalendarDocItem[]
  salesOrders: WorkCalendarDocItem[]
  tasks: WorkTaskListItem[]
}

export interface WorkTaskDetail extends WorkTaskListItem {
  objectType: string
  content?: string | null
  assigneeUserId: string
  assigneeUserName?: string | null
  createByUserId: string
  contactHistoryId?: string | null
}

export interface CustomerWorkTaskItem extends WorkTaskDetail {
  canWrite: boolean
}

export interface CustomerWorkTaskMonth {
  year: number
  month: number
  days: { date: string; taskCount: number }[]
}

export interface PagedCustomerWorkTasks {
  items: CustomerWorkTaskItem[]
  totalCount: number
  page: number
  pageSize: number
}

export interface WorkTaskCreateBody {
  objectId: string
  title: string
  content?: string
  startDate: string
  priority: number
  assigneeUserId?: string
}

export interface WorkTaskPatchBody {
  title?: string
  content?: string
  startDate?: string
  priority?: number
  assigneeUserId?: string
}

export interface WorkTaskAssigneeOption {
  id: string
  userName: string
  realName?: string | null
}

export const workCalendarApi = {
  month(year: number, month: number) {
    return apiClient.get<WorkCalendarMonth>('/api/v1/work-calendar/month', { params: { year, month } })
  },
  day(date: string) {
    return apiClient.get<WorkCalendarDay>('/api/v1/work-calendar/day', { params: { date } })
  },
  assignees() {
    return apiClient.get<WorkTaskAssigneeOption[]>('/api/v1/work-tasks/assignees')
  },
  create(body: WorkTaskCreateBody) {
    return apiClient.post<WorkTaskDetail>('/api/v1/work-tasks', body)
  },
  patch(id: string, body: WorkTaskPatchBody) {
    return apiClient.patch<WorkTaskDetail>(`/api/v1/work-tasks/${encodeURIComponent(id)}`, body)
  },
  start(id: string) {
    return apiClient.post<WorkTaskDetail>(`/api/v1/work-tasks/${encodeURIComponent(id)}/start`)
  },
  complete(id: string) {
    return apiClient.post<WorkTaskDetail>(`/api/v1/work-tasks/${encodeURIComponent(id)}/complete`)
  },
  cancel(id: string) {
    return apiClient.post<WorkTaskDetail>(`/api/v1/work-tasks/${encodeURIComponent(id)}/cancel`)
  },
  customerMonth(customerId: string, year: number, month: number, includeCancelled: boolean) {
    return apiClient.get<CustomerWorkTaskMonth>(
      `/api/v1/customers/${encodeURIComponent(customerId)}/work-tasks/month`,
      { params: { year, month, includeCancelled } }
    )
  },
  customerList(
    customerId: string,
    params: { page: number; pageSize?: number; startDate?: string; includeCancelled?: boolean }
  ) {
    return apiClient.get<PagedCustomerWorkTasks>(
      `/api/v1/customers/${encodeURIComponent(customerId)}/work-tasks`,
      { params }
    )
  },
  customerGet(customerId: string, taskId: string) {
    return apiClient.get<CustomerWorkTaskItem>(
      `/api/v1/customers/${encodeURIComponent(customerId)}/work-tasks/${encodeURIComponent(taskId)}`
    )
  }
}
