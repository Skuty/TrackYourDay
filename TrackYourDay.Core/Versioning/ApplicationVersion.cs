﻿namespace TrackYourDay.Core.Versioning
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
                var splittedVersion = version.Split('.');
                this.major = int.Parse(splittedVersion[0]);
                this.minor = int.Parse(splittedVersion[1]);
                this.patch = int.Parse(splittedVersion[2]);
            } catch (Exception e)
            {
                throw new ArgumentException("Version {version} is not in supported format.", version);
            }
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

            if ( this.patch > versionToCompare.patch)
            {
                return true;
            }

            return false;
        }
    }
}