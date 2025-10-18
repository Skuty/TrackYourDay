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
                var versionWithoutSPrefix = version.Replace("v", string.Empty);
                var parts = versionWithoutSPrefix.Split('-');
                var versionParts = parts[0].Split('.');

                this.major = int.Parse(versionParts[0]);
                this.minor = int.Parse(versionParts[1]);
                this.patch = int.Parse(versionParts[2]);

                if (parts.Length > 1)
                {
                    this.prerelease = parts[1];
                }
            }
            catch (Exception)
            {
                throw new ArgumentException($"Version {version} is not in a supported format.");
            }
        }

        public ApplicationVersion(Version version, string? prerelease = null)
        {
            this.major = version.Major;
            this.minor = version.Minor;
            this.patch = version.Build > 0 ? version.Build : 0;
            this.prerelease = prerelease;
        }

        public override string ToString()
        {
            return this.prerelease == null
                ? $"{this.major}.{this.minor}.{this.patch}"
                : $"{this.major}.{this.minor}.{this.patch}-{this.prerelease}";
        }

        public bool IsNewerThan(ApplicationVersion versionToCompare)
        {
            if (this.major > versionToCompare.major)
            {
                return true;
            }

            if (this.minor > versionToCompare.minor)
            {
                return true;
            }

            if (this.patch > versionToCompare.patch)
            {
                return true;
            }

            if (this.prerelease == null && versionToCompare.prerelease != null)
            {
                return true; // Stable versions are newer than prerelease versions
            }

            if (this.prerelease != null && versionToCompare.prerelease != null)
            {
                return string.Compare(this.prerelease, versionToCompare.prerelease, StringComparison.Ordinal) > 0;
            }

            return false;
        }
    }
}