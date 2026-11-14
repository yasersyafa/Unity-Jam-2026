# A Good Team Web template

Minimal charcoal-and-cream comic-style loading screen for Unity 6000.6 Web builds. All fonts and visual assets are local. Font licensing is included in `TemplateData/OFL.txt`.

Select **AGoodTeam** in Web Player Settings → Resolution and Presentation → Web Template. The project's Web - Desktop - Release profile also selects this template. Default canvas size is 960 × 540; Unity UI reference resolution remains 1920 × 1080. The HTML canvas fits the available viewport at 16:9, with black letterboxing when necessary.

`TemplateData/loading.js` receives real progress from `createUnityInstance`. At 100% it shows Preparing… until initialization resolves, then removes the overlay after a 200 ms transition. It imposes no minimum loading time. Reduced-motion users receive no decorative animation or exit delay. Errors display as plain text with a reload button.

Unity's engine splash remains enabled and is independent of the HTML loading screen. Test its timing in the exported build; the template does not manufacture or delay a Unity splash.

To change branding, edit the text in `index.html` and the palette variables in `TemplateData/style.css`. Preserve Unity's build macros and conditional directives, including progressive asset loading support.

Build into an ignored directory such as `Build/AGoodTeam`. Serve the output over HTTP with the correct content encoding for compressed build files; do not open index.html via file://. For itch.io, use a 960 × 540 embed with fullscreen enabled.
