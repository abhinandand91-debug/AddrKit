# Asset Store submission checklist

Done (by tooling, verified):
- [x] UPM package layout, naming `com.abhinandan.addrkit`, semver, `unity` field, author, documentation/changelog/license URLs
- [x] Every file and folder has a `.meta` (hidden `~` folders excepted), no orphan metas
- [x] Short paths, LF line endings, no archives or executables
- [x] Namespaces on all public types (runtime, editor, tests, samples)
- [x] Docs: README, QuickStart, Concepts, Lifecycle, Troubleshooting, CHANGELOG
- [x] Tests: 33 edit mode + 19 play mode pass
- [x] Real builds: macOS Mono, WebGL IL2CPP (run in browser), Android IL2CPP (build only)
- [x] Key images and 7 screenshots in `out/`

Needs you (requires your Unity account / decisions):
- [ ] **Run the official Validator.** Install "Asset Store Publishing Tools" from the Asset Store
      (https://assetstore.unity.com/packages/package/5368745), then Window > Tools > Asset Store > Validator, Validation Type = UPM.
      It needs a signed-in account, so it could not be run by the tooling here. Fix or disclose anything it flags.
- [ ] **License.** `LICENSE` is a placeholder MIT. Asset Store sales use Unity's standard EULA. Decide: free open-source, or paid asset
      (then replace/remove the MIT file in the package and keep the repo's license consistent with your plan).
- [ ] **Publisher Portal draft**: create the UPM product draft and reserve the technical name `com.abhinandan.addrkit`.
- [ ] **Upload**: Window > Tools > Asset Store > Uploader > UPM Packages > Upload.
- [ ] **Listing**: paste `LISTING.md`; upload icon (160x160), card (420x280), cover (1950x1300), social (1200x630), screenshots.
- [ ] **Support email / website**, price, and release notes (use CHANGELOG).
- [ ] Optional but recommended: test on a second machine / Unity version (6000.0) and on Windows, then update the platforms list.

Size limit for UPM products: 550 MB per the docs (the guidelines page says 700 MB). The package is under 1 MB.
