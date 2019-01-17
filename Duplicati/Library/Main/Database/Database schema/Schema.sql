/*
Syntax notes (Applying to schema.sql and versioning x.*.sql files):
Be careful with semicolons, it is used as a simple Split-point for statements.
For conditional schema statements, a preprocesossor exists. Example:
{#if sqlite_version >= 3.8.2} DO_SOMETHING {#else} DO_SOMETHING_ELSE {#endif}
Variables available: sqlite_version (type Version) and db_version (type int)
Nesting is possible when appending a number in the form "_x" to the #if #else #endif.
{#if sqlite_version >= 3.8.2} DO_SOMETHING_3.8 {#else} {#if_1 sqlite_version >= 3.6.5} DO_SOMETHING_3.6 {#else_1} DO_SOMETHING_ELSE {#endif_1} {#endif}
*/

/* 
The operation table is a local table 
that is used to record all operations
for later debug inspection, and can be
used to map log messages to an operation
*/
CREATE TABLE "Operation" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"Description" TEXT NOT NULL,
	"Timestamp" INTEGER NOT NULL
);

/*
The remote volumes table keeps track
of the state of all known volumes
*/
CREATE TABLE "Remotevolume" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"OperationID" INTEGER NOT NULL,
	"Name" NVARCHAR(450) NOT NULL,
	"Type" TEXT NOT NULL,
	"Size" INTEGER NULL,
	"Hash" TEXT NULL,
	"State" NVARCHAR(450) NOT NULL,
	"VerificationCount" INTEGER NOT NULL,
	"DeleteGraceTime" INTEGER NOT NULL
);

/* Index for detecting broken states */
CREATE UNIQUE INDEX "RemotevolumeName" ON "Remotevolume" ("Name", "State");

/*
The index-block table contains
references that explains what block
files a index file references.
This is used to remove index volumes,
when they no longer reference any
block volumes	
*/
CREATE TABLE "IndexBlockLink" (
	"IndexVolumeID" INTEGER NOT NULL,
	"BlockVolumeID" INTEGER NOT NULL
);

/*
The fileset collects all files belonging to 
a particular backup, and thus a remote Fileset
*/
CREATE TABLE "Fileset" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"OperationID" INTEGER NOT NULL,
	"VolumeID" INTEGER NOT NULL,
	"Timestamp" INTEGER NOT NULL
);

/*
The OperationFileset contains an
entry for each file scanned for
a single operation. The scantime
is the time the file was last 
scanned in UNIX EPOCH format
*/

CREATE TABLE "FilesetEntry" (
	"FilesetID" INTEGER NOT NULL,
	"FileID" INTEGER NOT NULL,
	"Lastmodified" INTEGER NOT NULL,
	CONSTRAINT "FilesetEntry_PK_FilesetIdFileId" PRIMARY KEY ("FilesetID", "FileID")
);

/* Improved reverse lookup for joining Fileset and File table */
CREATE INDEX "FilesetentryFileIdIndex" on "FilesetEntry" ("FileID");



/*
The FileEntry contains an ID
for each path and each version
of the data and metadata
*/
CREATE TABLE "File" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"Path" NVARCHAR(450) NOT NULL,
	"BlocksetID" INTEGER NOT NULL,
	"MetadataID" INTEGER NOT NULL
);

/* Fast path based lookup */
CREATE UNIQUE INDEX "FilePath" ON "File" ("Path", "BlocksetID", "MetadataID");

/*
The blocklist hashes are hashes of
fragments of the blocklists.
They are grouped by the BlocksetID
and ordered by the index
*/
CREATE TABLE "BlocklistHash" (
	"BlocksetID" INTEGER NOT NULL,
	"Index" INTEGER NOT NULL,
	"Hash" NVARCHAR(450) NOT NULL
);

/* Faster database recreation */
CREATE UNIQUE INDEX "BlocklistHashBlocksetIDIndex" ON "BlocklistHash" ("BlocksetID", "Index");

/*
The blockset is a list of blocks
Note that Length is actually redundant,
it can be calculated by 
SUM(Blockset.Size)
The FullHash is the hash of the entire
blob when reconstructed
*/
CREATE TABLE "Blockset" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"Length" INTEGER NOT NULL,
	"FullHash" NVARCHAR(450) NOT NULL
);

CREATE UNIQUE INDEX "BlocksetFullHash" ON "Blockset" ("FullHash", "Length");

/*
The elements of a blocklist,
the hash is the block hash,
they are grouped by the BlocksetID
and ordered by the index
For general speed and storage improvement 
we use a table with option "WITHOUT ROWID"
["WITHOUT ROWID" available since SQLite v3.8.2 (= System.Data.SQLite v1.0.90.0, rel 2013-12-23)]
*/
  
CREATE TABLE "BlocksetEntry" (
	"BlocksetID" INTEGER NOT NULL,
	"Index" INTEGER NOT NULL,
	"BlockID" INTEGER NOT NULL,
	CONSTRAINT "BlocksetEntry_PK_IdIndex" PRIMARY KEY ("BlocksetID", "Index")
);

/* As this table is a cross table we need fast lookup */
CREATE INDEX "BlocksetEntry_IndexIdsBackwards" ON "BlocksetEntry" ("BlockID");


/*
The individual block hashes,
mapped to the containing remote volume
*/
CREATE TABLE "Block" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
    "Hash" NVARCHAR(450) NOT NULL,
	"Size" INTEGER NOT NULL,
	"VolumeID" INTEGER NOT NULL
);

/* This is the most performance critical part of the database */
CREATE UNIQUE INDEX "BlockHashSize" ON "Block" ("Hash", "Size");

/* Add index for faster volume based block access (for compacting) */
CREATE INDEX "Block_IndexByVolumeId" ON "Block" ("VolumeID");

/*
The deleted block hashes,
mapped to the containing file,
used for wasted space computations
*/
CREATE TABLE "DeletedBlock" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
    "Hash" NVARCHAR(450) NOT NULL,
	"Size" INTEGER NOT NULL,
	"VolumeID" INTEGER NOT NULL
);

/*
If extra copies of blocks are detected, 
they are recorded here
*/
CREATE TABLE "DuplicateBlock" (
    "BlockID" INTEGER NOT NULL,
    "VolumeID" INTEGER NOT NULL
);

/*
A metadata set, essentially a placeholder
to easily extend metadatasets with new properties
*/
CREATE TABLE "Metadataset" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"BlocksetID" INTEGER NOT NULL
);

CREATE INDEX "MetadatasetBlocksetID" ON "Metadataset" ("BlocksetID");

/*
Operations performed on the backend,
intended to be used when constructing
an error report or when debugging
*/
CREATE TABLE "RemoteOperation" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"OperationID" INTEGER NOT NULL,
	"Timestamp" INTEGER NOT NULL,
	"Operation" TEXT NOT NULL,
	"Path" TEXT NOT NULL,
	"Data" BLOB NULL
);

/*
Logged events, intended to be used when 
constructing an error report or when 
debugging
*/
CREATE TABLE "LogData" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"OperationID" INTEGER NOT NULL,
	"Timestamp" INTEGER NOT NULL,
	"Type" TEXT NOT NULL,
	"Message" TEXT NOT NULL,
	"Exception" TEXT NULL
);

/*
Internal version tracking
*/
CREATE TABLE "Version" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1,1),
    "Version" INTEGER NOT NULL
);

/*
Settings, such as hash and blocksize,
used for verification
*/
CREATE TABLE "Configuration" (
	"Key" TEXT PRIMARY KEY NOT NULL IDENTITY(1,1),
	"Value" TEXT NOT NULL
);

INSERT INTO "Version" ("Version") VALUES (7);
