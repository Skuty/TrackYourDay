namespace TrackYourDay.Core.Versioning
{
    public class ApplicationVersion
    {
        private int major;
        private int minor;
        private int patch;
        public bool IsPrerelease { get; private set; }

        public ApplicationVersion(int major, int minor, int patch, bool isPrerelease)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
            this.IsPrerelease = isPrerelease;
        }

        public ApplicationVersion(string version, bool isPrerelease)
        {
            try
            {
                var versionWithoutSPrefix = version.Replace("v", string.Empty);
                var splittedVersion = versionWithoutSPrefix.Split('.');
                this.major = int.Parse(splittedVersion[0]);
                this.minor = int.Parse(splittedVersion[1]);
                this.patch = int.Parse(splittedVersion[2]);
                this.IsPrerelease = isPrerelease;
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Version {version} is not in supported format.", nameof(version));
            }
        }

        public ApplicationVersion(Version version, bool isPrerelease)
        {
            this.major = version.Major;
            this.minor = version.Minor;
            this.patch = version.Build > 0 ? version.Build : 0;
            this.IsPrerelease = isPrerelease;
        }

        public override string ToString()
        {
            var version = $"{this.major}.{this.minor}.{this.patch}";
            return IsPrerelease ? $"{version}-beta" : version;
        }

        public bool IsNewerThan(ApplicationVersion versionToCompare)
        {
            if (this.major > versionToCompare.major)
            {
                return true;
            }
            if (this.major < versionToCompare.major)
            {
                return false;
            }

            if (this.minor > versionToCompare.minor)
            {
                return true;
            }
            if (this.minor < versionToCompare.minor)
            {
                return false;
            }

            if (this.patch > versionToCompare.patch)
            {
                return true;
            }

            return false;
        }
    }
}