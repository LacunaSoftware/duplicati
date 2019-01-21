/*
 * The primary table that stores all backups.
 *
 * The name and tag are free form user strings.
 * The tags are comma separated
 * the TargetURL is the url to remote storage,
 * and the DBPath is the path to the local database
 */
CREATE TABLE "Backup" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Name" NVARCHAR(100) NOT NULL,
    "Description" TEXT NOT NULL DEFAULT '',
    "Tags" NVARCHAR(450) NOT NULL,
    "TargetURL" TEXT NOT NULL,
    "DBPath" TEXT NOT NULL
);

/*
 * The table that stores all schedules
 * 
 * Tags is a comma separated parsed field that indicates 
 * which backups to run when activated.
 * special tags are ID:1 which means backup with ID = 1
 * 
 * Time is the scheduled time, and lastRun is the last time the backup was executed
 *
 * Rule is a special parsed field
 */
CREATE TABLE "Schedule" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Tags" NVARCHAR(450) NOT NULL,
    "Time" INTEGER NOT NULL,
    "Repeat" TEXT NOT NULL,
    "LastRun" BIGINT NOT NULL,
    "Rule" TEXT NOT NULL
);

/*
 * The source table is a list of source folders and files
 */
CREATE TABLE "Source" (
    "BackupID" INTEGER NOT NULL,
    "Path" NVARCHAR(450) NOT NULL
);

/*
 * The filter table contains all filters associated with a backup.
 * The special backupID -1 means "applied to all backups"
 * The expression is the filter, if the filter is a regular
 * expression, it is surrounded by hard brackets [ ]
 */
CREATE TABLE "Filter" (
    "BackupID" INTEGER NOT NULL,
    "Order" INTEGER NOT NULL,
    "Include" INTEGER NOT NULL,
    "Expression" TEXT NOT NULL
);

/*
 * All options are stored in this table
 *
 * The special backupID -1 means "applied to all backups".
 *
 * The filter is used to indicate what the option applies to,
 * for instance backend:s3 will only apply to backends of type S3
 *
 * The name and value are the option name and value
 */
CREATE TABLE "Option" (
    "BackupID" INTEGER NOT NULL,
    "Filter" TEXT NOT NULL,
    "Name" NVARCHAR(450) NOT NULL,
    "Value" NVARCHAR(450) NOT NULL
);

/*
 * Recorded metadata about a backup
 * This table contains metadata, such as when the backup was last started,
 * how long it took, how many files there were, how big the backup set was,
 * how much data was uploaded, downloaded, how fast, how much space is left,
 * and similar data. Programs can use this information to improve the display,
 * but cannot count on these values being present
 */
CREATE TABLE "Metadata" (
    "BackupID" INTEGER NOT NULL,
    "Name" NVARCHAR(450) NOT NULL,
    "Value" TEXT NOT NULL
);

/*
 * The log of operations initiated by the scheduler/user
 */
CREATE TABLE "Log" (
    "BackupID" INTEGER NOT NULL,
    "Description" TEXT NOT NULL,
    "Start" INTEGER NOT NULL,
    "Finish" INTEGER NOT NULL,
    "Result" TEXT NOT NULL,
    "SuggestedIcon" TEXT NOT NULL
);

/*
 * The log of errors
 */
CREATE TABLE "ErrorLog" (
    "BackupID" INTEGER,
    "Message" TEXT NOT NULL,
    "Exception" TEXT,
    "Timestamp" INTEGER NOT NULL
);

/*
Internal version tracking
*/
CREATE TABLE "Version" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Version" INTEGER NOT NULL
);

/*
Notifications not yet acknowledged by the user
*/
CREATE TABLE "Notification" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Type" TEXT NOT NULL,
    "Title" TEXT NOT NULL,
    "Message" TEXT NOT NULL, 
    "Exception" TEXT NOT NULL, 
    "BackupID" TEXT NULL,
    "Action" TEXT NOT NULL,
    "Timestamp" INTEGER NOT NULL,
    "LogEntryID" TEXT NULL,
    "MessageID" TEXT NULL,
    "MessageLogTag" TEXT NULL
);

/*
Key/value storage for frontends
*/
CREATE TABLE "UIStorage" (
    "Scheme" NVARCHAR(450) NOT NULL, 
    "Key" NVARCHAR(450) NOT NULL, 
    "Value" TEXT NOT NULL
);

/*
Long-term temporary file records
*/
CREATE TABLE "TempFile" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Origin" NVARCHAR(450) NOT NULL, 
    "Path" NVARCHAR(450) NOT NULL, 
    "Timestamp" INTEGER NOT NULL,
    "Expires" INTEGER NOT NULL
);

INSERT INTO "Version" ("Version") VALUES (6);

