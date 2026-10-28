# Showcase SVG icons

Original MIT-licensed, transparent 24 × 24 vector artwork for the native Showcase.
Every existing command and group has an explicit semantic assignment in
`MainForm.Icons.cs`; unrelated actions no longer share the placeholder document
icon. Repeated presentations of the same action retain the same artwork.

The SVG files use simple geometry, rounded strokes, `currentColor` and blue accent
marks. The sample's `IRibbonIcon` implementation reads these owned assets and
draws their geometry at the requested size with current theme/disabled colors.
It supports only these asset primitives and is not a general-purpose SVG API.
Gallery tile previews and the existing Mouse/Touch illustrations are preserved.

Use `--capture-command-icons <directory>` to export a contact sheet, explicit
command/group coverage list and generated tab/collapsed-group captures.
