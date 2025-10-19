namespace TrackYourDay.Core.Versioning
{
    public class ApplicationVersion
    {
        private int major;
        private int minor;
        private int patch;
        private string? prerelease;

        public ApplicationVersion(int major, int minor, int patch, string? prerelease = null)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.prerelease = prerelease;
        }

        public ApplicationVersion(string version)
        {
            try
            {
                var versionWithoutPrefix = version.Replace("v", string.Empty);

                // Split on '-' to separate version from prerelease identifier
                var parts = versionWithoutPrefix.Split('-', 2);
                var versionPart = parts[0];
                this.prerelease = parts.Length > 1 ? parts[1] : null;

                // Parse the main version numbers
                var splittedVersion = versionPart.Split('.');
                this.major = int.Parse(splittedVersion[0]);
                this.minor = int.Parse(splittedVersion[1]);
                this.patch = int.Parse(splittedVersion[2]);
            } catch (Exception e)
            {
                throw new ArgumentException($"Version {version} is not in supported format.", version, e);
            }
        }

        public ApplicationVersion(Version version)
        {
            this.major = version.Major;
            this.minor = version.Minor;
            this.patch = version.Build > 0 ? version.Build : 0;
            this.prerelease = null;
        }

        public bool IsPrerelease => !string.IsNullOrEmpty(prerelease);

        public override string ToString()
        {
            var baseVersion = $"{this.major}.{this.minor}.{this.patch}";
            return string.IsNullOrEmpty(prerelease) ? baseVersion : $"{baseVersion}-{prerelease}";
        }

        public bool IsNewerThan(ApplicationVersion versionToCompare)
        {
            // Compare major version
            if (this.major != versionToCompare.major)
            {
                return this.major > versionToCompare.major;
            }

            // Compare minor version
            if (this.minor != versionToCompare.minor)
            {
                return this.minor > versionToCompare.minor;
            }

            // Compare patch version
            if (this.patch != versionToCompare.patch)
            {
                return this.patch > versionToCompare.patch;
            }

            // If base versions are equal, handle prerelease comparison
            // Stable version (1.0.0) is newer than prerelease (1.0.0-beta.1)
            if (this.prerelease == null && versionToCompare.prerelease != null)
            {
                return true;
            }

            // Prerelease is older than stable
            if (this.prerelease != null && versionToCompare.prerelease == null)
            {
                return false;
            }

            // Both are prereleases - compare lexicographically
            if (this.prerelease != null && versionToCompare.prerelease != null)
            {
                return string.Compare(this.prerelease, versionToCompare.prerelease, StringComparison.Ordinal) > 0;
            }

            // Versions are equal
            return false;
        }
    }
}