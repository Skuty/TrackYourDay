-- Manual migration: Add UNIQUE (Guid, TypeName) constraint to historical_data
-- 
-- Run this ONCE against %AppData%\TrackYourDay\TrackYourDay.db
-- BEFORE starting the app with the new build.
--
-- Tool: DB Browser for SQLite (https://sqlitebrowser.org/)
--   or: sqlite3.exe "%AppData%\TrackYourDay\TrackYourDay.db"

-- Step 1: Remove duplicate rows — keep the earliest (lowest Id) per Guid+TypeName
DELETE FROM historical_data
WHERE Id NOT IN (
    SELECT MIN(Id) FROM historical_data GROUP BY Guid, TypeName
);

-- Step 2: Rebuild table with UNIQUE constraint
--         (SQLite does not support ALTER TABLE ADD CONSTRAINT)
CREATE TABLE historical_data_new (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Guid TEXT NOT NULL,
    TypeName TEXT NOT NULL,
    DataJson TEXT NOT NULL,
    UNIQUE (Guid, TypeName)
);

INSERT INTO historical_data_new (Guid, TypeName, DataJson)
SELECT Guid, TypeName, DataJson FROM historical_data;

DROP TABLE historical_data;

ALTER TABLE historical_data_new RENAME TO historical_data;

CREATE INDEX IF NOT EXISTS idx_historical_data_type ON historical_data(TypeName);
