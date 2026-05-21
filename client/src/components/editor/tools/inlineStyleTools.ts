type InlineToolArgs = {
  api?: any
  config?: any
}

type InlineStyleOption = {
  label: string
  value: string
}

type InlineStyleMap = Record<string, string>

const DEFAULT_FONT_OPTIONS: InlineStyleOption[] = [
  {
    label: 'Default',
    value:
      'Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif',
  },
  {
    label: 'Serif',
    value: 'Georgia, Cambria, "Times New Roman", Times, serif',
  },
  {
    label: 'Mono',
    value:
      '"JetBrains Mono", "SFMono-Regular", Consolas, "Liberation Mono", Menlo, Monaco, monospace',
  },
  {
    label: 'Arial',
    value: 'Arial, Helvetica, sans-serif',
  },
  {
    label: 'Times',
    value: '"Times New Roman", Times, serif',
  },
  {
    label: 'Courier',
    value: '"Courier New", Courier, monospace',
  },
]

const DEFAULT_SIZE_OPTIONS: InlineStyleOption[] = [
  { label: '12', value: '12px' },
  { label: '14', value: '14px' },
  { label: '16', value: '16px' },
  { label: '18', value: '18px' },
  { label: '20', value: '20px' },
  { label: '24', value: '24px' },
  { label: '28', value: '28px' },
  { label: '32', value: '32px' },
  { label: '40', value: '40px' },
  { label: '48', value: '48px' },
]

function getCurrentRange(): Range | null {
  const selection = window.getSelection()

  if (!selection || selection.rangeCount === 0) {
    return null
  }

  const range = selection.getRangeAt(0)

  if (!range || range.collapsed) {
    return null
  }

  return range
}

function restoreRange(range: Range | null) {
  if (!range) return

  const selection = window.getSelection()
  if (!selection) return

  selection.removeAllRanges()
  selection.addRange(range)
}

function selectNodeContents(node: Node) {
  const selection = window.getSelection()
  if (!selection) return

  const range = document.createRange()
  range.selectNodeContents(node)

  selection.removeAllRanges()
  selection.addRange(range)
}

function applyStyleToRange(
  range: Range,
  styles: InlineStyleMap,
  dataType: string
) {
  if (!range || range.collapsed) return

  const selectedText = range.toString()
  if (!selectedText) return

  const span = document.createElement('span')
  span.className = 'editor-inline-style'
  span.setAttribute('data-editor-inline-style', dataType)

  Object.entries(styles).forEach(([property, value]) => {
    span.style.setProperty(property, value)
  })

  const content = range.extractContents()
  span.appendChild(content)
  range.insertNode(span)

  selectNodeContents(span)
}

class DropdownInlineStyleTool {
  protected api: any
  protected config: any
  protected savedRange: Range | null = null

  static get isInline() {
    return true
  }

  static get sanitize() {
    return {
      span: {
        class: true,
        style: true,
        'data-editor-inline-style': true,
      },
      strong: true,
      b: true,
      em: true,
      i: true,
      a: {
        href: true,
        target: true,
        rel: true,
      },
      mark: {
        class: true,
      },
      code: {
        class: true,
      },
    }
  }

  constructor({ api, config }: InlineToolArgs) {
    this.api = api
    this.config = config ?? {}
  }

  render(): HTMLElement {
    const select = document.createElement('select')
    select.className = 'ce-inline-style-select'
    select.title = this.config.title ?? 'Text style'

    const placeholder = document.createElement('option')
    placeholder.value = ''
    placeholder.textContent = this.config.placeholder ?? 'Style'
    placeholder.disabled = true
    placeholder.selected = true
    select.appendChild(placeholder)

    this.getOptions().forEach((option) => {
      const item = document.createElement('option')
      item.value = option.value
      item.textContent = option.label
      select.appendChild(item)
    })

    select.addEventListener('mousedown', (event) => {
      event.stopPropagation()
      this.saveSelection()
    })

    select.addEventListener('click', (event) => {
      event.stopPropagation()
      this.saveSelection()
    })

    select.addEventListener('change', () => {
      const value = select.value

      this.applyValue(value)
      select.selectedIndex = 0
    })

    return select
  }

  surround(range: Range) {
    this.saveSelection(range)
  }

  checkState() {
    return false
  }

  protected getOptions(): InlineStyleOption[] {
    return []
  }

  protected getStyle(_value: string): InlineStyleMap {
    return {}
  }

  protected getDataType(): string {
    return 'style'
  }

  protected saveSelection(range?: Range) {
    const targetRange = range && !range.collapsed ? range : getCurrentRange()

    if (targetRange) {
      this.savedRange = targetRange.cloneRange()
    }

    try {
      this.api?.selection?.save?.()
    } catch {
      // fallback bằng savedRange
    }
  }

  protected restoreSelection(): Range | null {
    try {
      this.api?.selection?.restore?.()
    } catch {
      // fallback bằng savedRange
    }

    if (this.savedRange) {
      restoreRange(this.savedRange)
      return this.savedRange
    }

    return getCurrentRange()
  }

  protected applyValue(value: string) {
    if (!value) return

    const range = this.restoreSelection()
    if (!range) return

    applyStyleToRange(range, this.getStyle(value), this.getDataType())
    this.savedRange = null
  }
}

export class FontFamilyInlineTool extends DropdownInlineStyleTool {
  protected getOptions(): InlineStyleOption[] {
    return this.config.options ?? DEFAULT_FONT_OPTIONS
  }

  protected getStyle(value: string): InlineStyleMap {
    return {
      'font-family': value,
    }
  }

  protected getDataType(): string {
    return 'font-family'
  }
}

export class FontSizeInlineTool extends DropdownInlineStyleTool {
  protected getOptions(): InlineStyleOption[] {
    return this.config.options ?? DEFAULT_SIZE_OPTIONS
  }

  protected getStyle(value: string): InlineStyleMap {
    return {
      'font-size': value,
      'line-height': '1.65',
    }
  }

  protected getDataType(): string {
    return 'font-size'
  }
}

class ColorInlineStyleTool {
  protected api: any
  protected config: any
  protected savedRange: Range | null = null

  static get isInline() {
    return true
  }

  static get sanitize() {
    return {
      span: {
        class: true,
        style: true,
        'data-editor-inline-style': true,
      },
      strong: true,
      b: true,
      em: true,
      i: true,
      a: {
        href: true,
        target: true,
        rel: true,
      },
      mark: {
        class: true,
      },
      code: {
        class: true,
      },
    }
  }

  constructor({ api, config }: InlineToolArgs) {
    this.api = api
    this.config = config ?? {}
  }

  render(): HTMLElement {
    const button = document.createElement('button')
    button.type = 'button'
    button.className = `ce-inline-tool ce-inline-color-tool ${this.getButtonClass()}`
    button.title = this.config.title ?? 'Color'

    const label = document.createElement('span')
    label.className = 'ce-inline-color-label'
    label.textContent = this.getLabel()

    const swatch = document.createElement('span')
    swatch.className = 'ce-inline-color-swatch'
    swatch.style.backgroundColor = this.getDefaultColor()

    const input = document.createElement('input')
    input.type = 'color'
    input.className = 'ce-inline-color-input'
    input.value = this.getDefaultColor()
    input.tabIndex = -1

    button.appendChild(label)
    button.appendChild(swatch)
    button.appendChild(input)

    button.addEventListener('mousedown', (event) => {
      event.preventDefault()
      event.stopPropagation()
      this.saveSelection()
    })

    button.addEventListener('click', (event) => {
      event.preventDefault()
      event.stopPropagation()
      this.saveSelection()
      input.click()
    })

    input.addEventListener('input', () => {
      swatch.style.backgroundColor = input.value
    })

    input.addEventListener('change', () => {
      swatch.style.backgroundColor = input.value
      this.applyColor(input.value)
    })

    return button
  }

  surround(range: Range) {
    this.saveSelection(range)
  }

  checkState() {
    return false
  }

  protected getButtonClass(): string {
    return ''
  }

  protected getLabel(): string {
    return 'A'
  }

  protected getDefaultColor(): string {
    return '#ffffff'
  }

  protected getDataType(): string {
    return 'color'
  }

  protected getStyle(color: string): InlineStyleMap {
    return {
      color,
    }
  }

  protected saveSelection(range?: Range) {
    const targetRange = range && !range.collapsed ? range : getCurrentRange()

    if (targetRange) {
      this.savedRange = targetRange.cloneRange()
    }

    try {
      this.api?.selection?.save?.()
    } catch {
      // fallback bằng savedRange
    }
  }

  protected restoreSelection(): Range | null {
    try {
      this.api?.selection?.restore?.()
    } catch {
      // fallback bằng savedRange
    }

    if (this.savedRange) {
      restoreRange(this.savedRange)
      return this.savedRange
    }

    return getCurrentRange()
  }

  protected applyColor(color: string) {
    const range = this.restoreSelection()
    if (!range) return

    applyStyleToRange(range, this.getStyle(color), this.getDataType())
    this.savedRange = null
  }
}

export class TextColorInlineTool extends ColorInlineStyleTool {
  protected getButtonClass(): string {
    return 'ce-inline-text-color-tool'
  }

  protected getLabel(): string {
    return 'A'
  }

  protected getDefaultColor(): string {
    return '#f1f1f1'
  }

  protected getDataType(): string {
    return 'text-color'
  }

  protected getStyle(color: string): InlineStyleMap {
    return {
      color,
    }
  }
}

export class HighlightColorInlineTool extends ColorInlineStyleTool {
  protected getButtonClass(): string {
    return 'ce-inline-highlight-color-tool'
  }

  protected getLabel(): string {
    return 'H'
  }

  protected getDefaultColor(): string {
    return '#5a4515'
  }

  protected getDataType(): string {
    return 'highlight-color'
  }

  protected getStyle(color: string): InlineStyleMap {
    return {
      'background-color': color,
      'border-radius': '4px',
      padding: '0.02em 0.18em',
    }
  }
}