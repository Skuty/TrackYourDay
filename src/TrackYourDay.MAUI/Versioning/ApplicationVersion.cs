namespace TrackYourDay.MAUI.Versioning
{
    public class ApplicationVersion
    {
        private int major;
        private int minor;
        private int patch;

        public ApplicationVersion(int major, int minor, int patch)
        {
            this.major = major;
            this.minor = minor;
            this.patch = patch;
        }

        public ApplicationVersion(Version version)
        {
            this.major = version.Major;
            this.minor = version.Minor;
            this.patch = version.Build > 0 ? version.Build : 0;
        }

        public ApplicationVersion(string version)
        {
            try
            {
                var versionWithoutSPrefix = version.Replace("v", string.Empty);
                var splittedVersion = versionWithoutSPrefix.Split('.');
                major = int.Parse(splittedVersion[0]);
                minor = int.Parse(splittedVersion[1]);
                patch = int.Parse(splittedVersion[2]);
            }
            catch (Exception e)
            {
                throw new ArgumentException("Version {version} is not in supported format.", version);
            }
        }

        public override string ToString()
        {
            return $"{major}.{minor}.{patch}";
        }

        public bool IsNewerThan(ApplicationVersion versionToCompare)
        {
            if (major > versionToCompare.major)
            {
                return true;
            }

            if (minor > versionToCompare.minor)
            {
                return true;
            }

            //TODO fix this for scenario 0.1.0 and 0.0.8
            if (patch > versionToCompare.patch)
            {
                return true;
            }

            return false;
        }
    }
}