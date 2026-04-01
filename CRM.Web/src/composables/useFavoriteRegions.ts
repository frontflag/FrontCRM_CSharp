import { ref, readonly, type Ref } from 'vue';

export interface FavoriteRegion {
  path: string[];
  /** 展示用：省 / 市 / 区 */
  label: string;
}

const STORAGE_KEY = 'crm_region_favorites_v1';
const MAX_ITEMS = 12;

const favorites: Ref<FavoriteRegion[]> = ref([]);
let loaded = false;

function pathKey(path: readonly string[]): string {
  return path.join('\u001f');
}

function loadFromStorage(): void {
  if (typeof localStorage === 'undefined') return;
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return;
    const parsed = JSON.parse(raw) as FavoriteRegion[];
    if (!Array.isArray(parsed)) return;
    favorites.value = parsed
      .filter((x) => x && Array.isArray(x.path) && x.path.length >= 2)
      .map((x) => ({
        path: x.path.map(String),
        label: x.path.join(' / ')
      }));
  } catch {
    /* ignore */
  }
}

function persist(): void {
  if (typeof localStorage === 'undefined') return;
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(favorites.value));
  } catch {
    /* ignore */
  }
}

function ensureLoaded(): void {
  if (loaded) return;
  loaded = true;
  loadFromStorage();
}

export function useFavoriteRegions() {
  ensureLoaded();

  function hasPath(path: readonly string[] | undefined): boolean {
    if (!path || path.length < 2) return false;
    const key = pathKey(path);
    return favorites.value.some((f) => pathKey(f.path) === key);
  }

  function addFromPath(path: readonly string[]): void {
    if (!path || path.length < 2) return;
    const label = path.join(' / ');
    const key = pathKey(path);
    const next = favorites.value.filter((f) => pathKey(f.path) !== key);
    next.unshift({ path: [...path], label });
    if (next.length > MAX_ITEMS) next.length = MAX_ITEMS;
    favorites.value = next;
    persist();
  }

  function removeByPath(path: readonly string[]): void {
    const key = pathKey(path);
    favorites.value = favorites.value.filter((f) => pathKey(f.path) !== key);
    persist();
  }

  /** 点击常用项时排到最前，便于下次更快找到 */
  function touchPath(path: readonly string[]): void {
    const key = pathKey(path);
    const idx = favorites.value.findIndex((f) => pathKey(f.path) === key);
    if (idx <= 0) return;
    const copy = [...favorites.value];
    const [item] = copy.splice(idx, 1);
    copy.unshift(item);
    favorites.value = copy;
    persist();
  }

  return {
    favorites: readonly(favorites),
    hasPath,
    addFromPath,
    removeByPath,
    touchPath,
    pathKey
  };
}
