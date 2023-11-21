namespace TrackYourDay.Core.Versioning
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

        public ApplicationVersion(string version)
        {
            try
            {
                var versionWithoutSPrefix = version.Replace("v", string.Empty);
                var splittedVersion = versionWithoutSPrefix.Split('.');
                this.major = int.Parse(splittedVersion[0]);
                this.minor = int.Parse(splittedVersion[1]);
                this.patch = int.Parse(splittedVersion[2]);
            } catch (Exception e)
            {
                throw new ArgumentException("Version {version} is not in supported format.", version);
            }
        }

        public override string ToString()
        {
            return $"{this.major}.{this.minor}.{this.patch}";
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

            //TODO fix this for scenario 0.1.0 and 0.0.8
            if ( this.patch > versionToCompare.patch)
            {
                return true;
            }

            return false;
        }
    }
}