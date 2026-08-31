export type IconKey =
  | 'house' | 'file' | 'folder' | 'article' | 'image' | 'tag' | 'user' | 'users'
  | 'mail' | 'star' | 'shield' | 'calendar' | 'bookmark' | 'globe' | 'layers' | 'grid'
  | 'layout' | 'video' | 'code' | 'chart' | 'link' | 'pin' | 'bell' | 'rocket' | 'settings'

const S = 'stroke="currentColor" fill="none" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round"'
const F = 'fill="currentColor" stroke="none"'

export const ICON_SVG: Record<IconKey, string> = {
  house:    `<path ${S} d="M2 8.5L8 2l6 6.5V15h-4v-4H6v4H2V8.5z"/>`,
  file:     `<path ${S} d="M4 1.5h5.5L13 5.5V15H4V1.5z"/><path ${S} d="M9.5 1.5V5.5H13"/>`,
  folder:   `<path ${S} d="M2 4.5h4l2 2h6v8H2V4.5z"/>`,
  article:  `<rect ${S} x="2" y="2" width="12" height="13" rx="1.5"/><line ${S} x1="5" y1="5.5" x2="11" y2="5.5"/><line ${S} x1="5" y1="8" x2="11" y2="8"/><line ${S} x1="5" y1="10.5" x2="8.5" y2="10.5"/>`,
  image:    `<rect ${S} x="2" y="3" width="12" height="10" rx="1.5"/><path ${S} d="M2 10l3.5-3.5 3 3 2-2 3.5 4"/><circle ${S} cx="11" cy="6.5" r="1"/>`,
  tag:      `<path ${S} d="M3 3h5.5L14 8.5 8.5 14 3 8.5V3z"/><circle ${F} cx="6" cy="6" r="1"/>`,
  user:     `<circle ${S} cx="8" cy="6" r="3"/><path ${S} d="M2 15c0-3 2.7-5.5 6-5.5s6 2.5 6 5.5"/>`,
  users:    `<circle ${S} cx="6" cy="7" r="2.5"/><path ${S} d="M1 15c0-2.5 2.2-4.5 5-4.5s5 2 5 4.5"/><path ${S} d="M11 4.5a2.5 2.5 0 010 5"/><path ${S} d="M13.5 15c0-2-1.5-3.5-3-4"/>`,
  mail:     `<rect ${S} x="2" y="4" width="12" height="9" rx="1.5"/><path ${S} d="M2 4l6 5.5L14 4"/>`,
  star:     `<path ${S} d="M8 1.5l1.8 3.8 4.2.6-3 3 .7 4.1L8 11l-3.7 2 .7-4.1-3-3 4.2-.6z"/>`,
  shield:   `<path ${S} d="M8 1.5L2 4v4.5c0 3 2.5 5.5 6 6.5 3.5-1 6-3.5 6-6.5V4L8 1.5z"/>`,
  calendar: `<rect ${S} x="2" y="3" width="12" height="12" rx="1.5"/><line ${S} x1="2" y1="7.5" x2="14" y2="7.5"/><line ${S} x1="5.5" y1="1.5" x2="5.5" y2="4.5"/><line ${S} x1="10.5" y1="1.5" x2="10.5" y2="4.5"/><rect ${F} x="5" y="9.5" width="1.5" height="1.5" rx=".3"/><rect ${F} x="9.5" y="9.5" width="1.5" height="1.5" rx=".3"/>`,
  bookmark: `<path ${S} d="M5 1.5h6v13l-3-2.5-3 2.5V1.5z"/>`,
  globe:    `<circle ${S} cx="8" cy="8" r="6"/><line ${S} x1="2" y1="8" x2="14" y2="8"/><path ${S} d="M8 2c-2 2-2.5 3.7-2.5 6s.5 4 2.5 6"/><path ${S} d="M8 2c2 2 2.5 3.7 2.5 6s-.5 4-2.5 6"/>`,
  layers:   `<path ${S} d="M2 10l6 3 6-3"/><path ${S} d="M2 7l6 3 6-3"/><path ${S} d="M2 4l6-2.5L14 4 8 7 2 4z"/>`,
  grid:     `<rect ${S} x="2" y="2" width="5" height="5" rx="1"/><rect ${S} x="9" y="2" width="5" height="5" rx="1"/><rect ${S} x="2" y="9" width="5" height="5" rx="1"/><rect ${S} x="9" y="9" width="5" height="5" rx="1"/>`,
  layout:   `<rect ${S} x="2" y="2" width="12" height="13" rx="1.5"/><line ${S} x1="2" y1="6.5" x2="14" y2="6.5"/><line ${S} x1="6.5" y1="6.5" x2="6.5" y2="15"/>`,
  video:    `<rect ${S} x="2" y="4" width="9" height="8" rx="1.5"/><path ${S} d="M11 6.5l3-2v7l-3-2"/>`,
  code:     `<path ${S} d="M5 4.5L1 8l4 3.5"/><path ${S} d="M11 4.5l4 3.5-4 3.5"/><line ${S} x1="9.5" y1="3" x2="6.5" y2="13"/>`,
  chart:    `<line ${S} x1="2" y1="14" x2="14" y2="14"/><rect ${S} x="3" y="9" width="2.5" height="5" rx=".5"/><rect ${S} x="6.8" y="5.5" width="2.5" height="8.5" rx=".5"/><rect ${S} x="10.5" y="7.5" width="2.5" height="6.5" rx=".5"/>`,
  link:     `<path ${S} d="M6.5 9.5a4 4 0 005.7 0l2-2a4 4 0 00-5.7-5.7L7.2 3.1"/><path ${S} d="M9.5 6.5a4 4 0 00-5.7 0l-2 2a4 4 0 005.7 5.7l1.3-1.3"/>`,
  pin:      `<path ${S} d="M8 1.5a4.5 4.5 0 014.5 4.5c0 3.5-4.5 8.5-4.5 8.5S3.5 9.5 3.5 6A4.5 4.5 0 018 1.5z"/><circle ${S} cx="8" cy="6" r="1.5"/>`,
  bell:     `<path ${S} d="M8 1.5a5 5 0 015 5v3.5l1.5 2H2L3.5 10V6.5a5 5 0 015-5z"/><path ${S} d="M6.5 13.5a1.5 1.5 0 003 0"/>`,
  rocket:   `<path ${S} d="M8 2C6 4.5 5 7 5 9h6c0-2-1-4.5-3-7z"/><path ${S} d="M5 9L3 11.5V13h2"/><path ${S} d="M11 9l2 2.5V13h-2"/><line ${S} x1="6.5" y1="9" x2="6.5" y2="13.5"/><line ${S} x1="9.5" y1="9" x2="9.5" y2="13.5"/>`,
  settings: `<circle ${S} cx="8" cy="8" r="2.5"/><path ${S} d="M8 1.5V3M8 13v1.5M1.5 8H3M13 8h1.5M3.2 3.2l1 1M11.8 11.8l1 1M3.2 12.8l1-1M11.8 4.2l1-1"/>`,
}

export const ICON_LABELS: Record<IconKey, string> = {
  house: 'House', file: 'Document', folder: 'Folder', article: 'Article',
  image: 'Image', tag: 'Tag', user: 'Person', users: 'Team', mail: 'Email',
  star: 'Featured', shield: 'Shield', calendar: 'Calendar', bookmark: 'Bookmark',
  globe: 'Globe', layers: 'Layers', grid: 'Grid', layout: 'Layout', video: 'Video',
  code: 'Code', chart: 'Chart', link: 'Link', pin: 'Location', bell: 'Alert',
  rocket: 'Launch', settings: 'Settings',
}

export const ALL_ICONS = Object.keys(ICON_SVG) as IconKey[]
