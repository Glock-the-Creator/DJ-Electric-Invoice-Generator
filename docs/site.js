const versionElement = document.getElementById("version");
const publishedElement = document.getElementById("published");
const notesElement = document.getElementById("release-notes");
const downloadLinkElement = document.getElementById("download-link");

async function loadReleaseManifest() {
  try {
    const response = await fetch("release.json", { cache: "no-store" });
    if (!response.ok) {
      throw new Error("release manifest unavailable");
    }

    const release = await response.json();
    if (release.version) {
      versionElement.textContent = release.version;
    }

    if (release.publishedAtUtc) {
      const publishedAt = new Date(release.publishedAtUtc);
      publishedElement.textContent = Number.isNaN(publishedAt.valueOf())
        ? release.publishedAtUtc
        : publishedAt.toLocaleString();
    }

    if (release.releaseNotes) {
      notesElement.textContent = release.releaseNotes;
    }

    if (release.installerUrl) {
      downloadLinkElement.href = release.installerUrl;
    }
  } catch {
    versionElement.textContent = "Not published yet";
    publishedElement.textContent = "Run the release script";
    notesElement.textContent = "Publish a release to generate release.json and the setup download.";
  }
}

loadReleaseManifest();
