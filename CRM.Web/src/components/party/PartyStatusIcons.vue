<script setup lang="ts">
import { computed } from 'vue';

const props = withDefaults(
  defineProps<{
    /** 用于 SVG defs id 去重（列表行 id、详情主键等） */
    entityId: string;
    frozen?: boolean;
    blacklist?: boolean;
    size?: 'sm' | 'md';
  }>(),
  { frozen: false, blacklist: false, size: 'md' }
);

function sanitizeId(id: string) {
  return (id || 'x').replace(/[^a-zA-Z0-9]/g, '_');
}

const snowflakeGradientId = computed(() => `party-snow-${sanitizeId(props.entityId)}`);
const skullIconMaskId = computed(() => `party-skull-${sanitizeId(props.entityId)}`);
</script>

<template>
  <span
    v-if="frozen || blacklist"
    class="party-status-icons"
    :class="`party-status-icons--${size}`"
  >
    <span
      v-if="frozen"
      class="party-status-icons__icon party-status-icons__icon--snow"
      title="冻结"
      aria-label="冻结"
    >
      <svg class="party-status-icons__svg party-status-icons__svg--frost-snow" viewBox="0 0 24 24" aria-hidden="true">
        <defs>
          <linearGradient
            :id="snowflakeGradientId"
            x1="12"
            y1="0.5"
            x2="12"
            y2="23.5"
            gradientUnits="userSpaceOnUse"
          >
            <stop offset="0%" stop-color="#ffffff" />
            <stop offset="35%" stop-color="#cffafe" />
            <stop offset="72%" stop-color="#67e8f9" />
            <stop offset="100%" stop-color="#0891b2" />
          </linearGradient>
        </defs>
        <g
          :stroke="`url(#${snowflakeGradientId})`"
          fill="none"
          stroke-linecap="round"
          stroke-linejoin="round"
          transform="translate(12, 12)"
        >
          <template v-for="arm in 6" :key="arm">
            <g :transform="`rotate(${(arm - 1) * 60})`">
              <path
                d="M0 0 L0 -8.6 M0 -3.2 L-2.05 -5.35 M0 -3.2 L2.05 -5.35 M0 -6.1 L-1.55 -7.85 M0 -6.1 L1.55 -7.85"
                stroke-width="1.45"
              />
            </g>
          </template>
          <circle cx="0" cy="0" r="1.4" :fill="`url(#${snowflakeGradientId})`" stroke="none" />
        </g>
      </svg>
    </span>
    <span
      v-if="blacklist"
      class="party-status-icons__icon party-status-icons__icon--skull"
      title="黑名单"
      aria-label="黑名单"
    >
      <svg class="party-status-icons__svg party-status-icons__svg--skull-crossbones" viewBox="0 0 24 24" aria-hidden="true">
        <defs>
          <mask :id="skullIconMaskId" maskUnits="userSpaceOnUse">
            <rect width="24" height="24" fill="black" />
            <path
              d="M4.1 19.6 L19.9 14.2"
              stroke="white"
              stroke-width="3.2"
              stroke-linecap="round"
              fill="none"
            />
            <path
              d="M19.9 19.6 L4.1 14.2"
              stroke="white"
              stroke-width="3.2"
              stroke-linecap="round"
              fill="none"
            />
            <path
              fill="white"
              d="M12 2.55c3.1 0 5.7 1.9 6.1 5.15.28 2.25-.12 4.05-1.2 5.45-.72.9-1.72 1.5-2.95 1.78-.12.42-.46.78-.95 1-.36.18-.75.27-1.12.27-.37 0-.76-.09-1.12-.27-.49-.22-.83-.58-.95-1-1.23-.28-2.23-.88-2.95-1.78-1.08-1.4-1.48-3.2-1.2-5.45.4-3.25 3-5.15 6.1-5.15z"
            />
            <circle cx="9" cy="8.25" r="1.58" fill="black" />
            <circle cx="15" cy="8.25" r="1.58" fill="black" />
            <path d="M12 9.35 L10.48 12.05h3.04z" fill="black" />
          </mask>
        </defs>
        <rect width="24" height="24" fill="currentColor" :mask="`url(#${skullIconMaskId})`" />
      </svg>
    </span>
  </span>
</template>

<style scoped lang="scss">
.party-status-icons {
  display: inline-flex;
  align-items: center;
  flex-shrink: 0;

  &--sm {
    gap: 4px;
    .party-status-icons__svg {
      width: 16px;
      height: 16px;
    }
  }

  &--md {
    gap: 8px;
    .party-status-icons__svg {
      width: 22px;
      height: 22px;
    }
  }

  &__svg {
    display: block;
  }

  &__svg--frost-snow,
  &__svg--skull-crossbones {
    overflow: visible;
  }

  &__icon--snow {
    display: inline-flex;
    color: rgba(140, 210, 240, 0.95);
    filter: drop-shadow(0 0 4px rgba(165, 243, 252, 0.55)) drop-shadow(0 0 10px rgba(34, 211, 238, 0.25));
  }

  &__icon--skull {
    display: inline-flex;
    color: #fff;
    filter: drop-shadow(0 0 0.5px rgba(0, 0, 0, 0.55)) drop-shadow(0 0 6px rgba(0, 0, 0, 0.22));
  }
}
</style>
