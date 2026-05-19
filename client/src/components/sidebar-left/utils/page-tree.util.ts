import type { Guid } from '@/api/models/common.model'
import type { PageResponse } from '@/api/models/page.model'
import type { PageTreeItem } from '../types/sidebar.types'

function sortPagesByDate(pages: PageTreeItem[]) {
  return [...pages].sort((a, b) => {
    return new Date(a.createdDate).getTime() - new Date(b.createdDate).getTime()
  })
}

function sortTree(nodes: PageTreeItem[]): PageTreeItem[] {
  return sortPagesByDate(nodes).map((node) => ({
    ...node,
    children: sortTree(node.children),
  }))
}

export function buildPageTree(pages: PageResponse[]): PageTreeItem[] {
  const map = new Map<Guid, PageTreeItem>()
  const roots: PageTreeItem[] = []

  pages.forEach((page) => {
    map.set(page.id, {
      ...page,
      children: [],
    })
  })

  map.forEach((page) => {
    if (page.parentPageId && map.has(page.parentPageId)) {
      map.get(page.parentPageId)?.children.push(page)
      return
    }

    roots.push(page)
  })

  return sortTree(roots)
}

function createPageTreeNode(page: PageResponse): PageTreeItem {
  return {
    ...page,
    children: [],
  }
}

export function insertPageIntoTree(
  tree: PageTreeItem[],
  page: PageResponse
): PageTreeItem[] {
  const node = createPageTreeNode(page)

  if (!page.parentPageId) {
    return [node, ...tree]
  }

  let inserted = false

  function insert(nodes: PageTreeItem[]): PageTreeItem[] {
    return nodes.map((item) => {
      if (item.id === page.parentPageId) {
        inserted = true

        return {
          ...item,
          children: [node, ...item.children],
        }
      }

      return {
        ...item,
        children: insert(item.children),
      }
    })
  }

  const nextTree = insert(tree)

  return inserted ? nextTree : [node, ...tree]
}
