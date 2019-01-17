CREATE TABLE "ChangeJournalData" (
	"ID" INTEGER PRIMARY KEY IDENTITY(1,1),
	"FilesetID" INTEGER NOT NULL,		
	"VolumeName" NVARCHAR(450) NOT NULL,			
	"JournalID" INTEGER NOT NULL,		
	"NextUsn" INTEGER NOT NULL, 		
	"ConfigHash" TEXT NOT NULL	
);

UPDATE "Version" SET "Version" = 8;
