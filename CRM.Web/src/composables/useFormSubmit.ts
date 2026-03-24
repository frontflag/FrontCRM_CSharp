import { ElMessage } from 'element-plus'
import type { FormInstance } from 'element-plus'
import type { Ref } from 'vue'
import { getApiErrorMessage } from '@/utils/apiError'

/** 表单校验失败时的默认提示（Element Plus validate() 失败会 reject，需 catch） */
export const DEFAULT_FORM_VALIDATE_WARN =
  '请先完成必填项（表单中带 * 或红色提示的字段）'

/** 保存成功后的默认提示 */
export const DEFAULT_SAVE_SUCCESS = '保存成功'

export type FormRef = Ref<FormInstance | undefined>

/**
 * 校验 Element Plus 表单；失败时弹出 warning，返回 false。
 */
export async function validateElFormOrWarn(
  formRef: FormRef,
  warnMessage: string = DEFAULT_FORM_VALIDATE_WARN
): Promise<boolean> {
  const inst = formRef.value
  if (!inst) return false
  try {
    await inst.validate()
    return true
  } catch {
    ElMessage.warning(warnMessage)
    return false
  }
}

export interface RunSaveTaskOptions<T> {
  task: () => Promise<T>
  /** 在成功提示之后调用（例如 router.push） */
  onSuccess?: (result: T) => void
  loading?: Ref<boolean>
  /** 与 formatSuccess 二选一；有 formatSuccess 时忽略此项 */
  successMessage?: string
  formatSuccess?: (result: T) => string
  errorMessage?: (error: unknown) => string
}

/**
 * 执行保存请求：loading、成功/失败提示、可选后续跳转。
 * 不含表单校验；可与 validateElFormOrWarn 组合使用。
 */
export async function runSaveTask<T = void>(
  options: RunSaveTaskOptions<T>
): Promise<T | undefined> {
  const loading = options.loading
  if (loading) loading.value = true
  try {
    const result = await options.task()
    const msg = options.formatSuccess
      ? options.formatSuccess(result)
      : (options.successMessage ?? DEFAULT_SAVE_SUCCESS)
    ElMessage.success(msg)
    options.onSuccess?.(result)
    return result
  } catch (error: unknown) {
    const msg = options.errorMessage
      ? options.errorMessage(error)
      : getApiErrorMessage(error, '保存失败')
    ElMessage.error(msg)
    return undefined
  } finally {
    if (loading) loading.value = false
  }
}

export interface RunValidatedFormSaveOptions<T> extends RunSaveTaskOptions<T> {
  validateWarnMessage?: string
  /** 在 el-form 通过后执行；返回 false 则中止（请自行已提示用户） */
  afterValidate?: () => boolean | Promise<boolean>
}

/**
 * 先校验表单，再执行与 runSaveTask 相同的保存与提示逻辑。
 */
export async function runValidatedFormSave<T = void>(
  formRef: FormRef,
  options: RunValidatedFormSaveOptions<T>
): Promise<T | undefined> {
  const ok = await validateElFormOrWarn(formRef, options.validateWarnMessage)
  if (!ok) return undefined

  if (options.afterValidate) {
    const pass = await options.afterValidate()
    if (!pass) return undefined
  }

  return runSaveTask<T>({
    task: options.task,
    onSuccess: options.onSuccess,
    loading: options.loading,
    successMessage: options.successMessage,
    formatSuccess: options.formatSuccess,
    errorMessage: options.errorMessage
  })
}
