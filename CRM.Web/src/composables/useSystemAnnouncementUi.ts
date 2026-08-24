import { ref } from 'vue'

/** 顶栏铃铛 / 强制弹窗 / 历史抽屉 共享 UI 状态 */
const messageDrawerOpen = ref(false)
const messageDrawerTab = ref<'messages' | 'announcements'>('announcements')
const unreadCount = ref(0)
const noticeUnreadCount = ref(0)
const hasUnreadUrgentNotice = ref(false)
const forceModalToken = ref(0)

export function useSystemAnnouncementUi() {
  function openMessageDrawer(tab: 'messages' | 'announcements' = 'announcements') {
    messageDrawerTab.value = tab
    messageDrawerOpen.value = true
  }

  function bumpForceModalCheck() {
    forceModalToken.value += 1
  }

  function setUnreadCount(n: number) {
    unreadCount.value = Math.max(0, n | 0)
  }

  function setNoticeUnreadSummary(unread: number, urgent: boolean) {
    noticeUnreadCount.value = Math.max(0, unread | 0)
    hasUnreadUrgentNotice.value = !!urgent && noticeUnreadCount.value > 0
  }

  return {
    messageDrawerOpen,
    messageDrawerTab,
    unreadCount,
    noticeUnreadCount,
    hasUnreadUrgentNotice,
    forceModalToken,
    openMessageDrawer,
    bumpForceModalCheck,
    setUnreadCount,
    setNoticeUnreadSummary
  }
}
