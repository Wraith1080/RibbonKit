# Native RibbonKit.WinForms

This solution is independent of the WPF solution in the parent checkout. Work on
the user's current branch; do not commit, push or change WPF runtime/build/CI.
Use normal Debug/Release output folders. Version and package identity are native.

- Use CLR properties/events, WinForms binding, owned component collections and
  public framework/Win32 APIs. No WPF hosting, peers, templates or dependency properties.
- Keep reusable behavior in the library. Samples and package consumers use public APIs.
- Keep model state separate from layout snapshots, painting, input and automation.
- Dispose owned models/popups/caches/subscriptions; borrow application icons and renderers.
- M1 uses opaque Office2024 light surfaces and immediate transitions. Mica, custom
  window chrome, animation, QAT, galleries and customization belong to later slices.
- Inspect all four rounded corners at representative DPI before claiming symmetry.
  Custom paths and .NET 11 styling are not proof that GDI rasterization is corrected.
- Distinguish unit/STA, external UIA, generated-code, actual VS designer, native
  display/hardware and human acceptance. Record pending gates in docs/M1-STATUS.md.
- Use one agent. Build/test this solution only; WPF checks do not verify WinForms.

See ../docs/21-NATIVE-WINFORMS-PLAN.md for the approved scope and API proposal.
