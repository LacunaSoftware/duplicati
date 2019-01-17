
CREATE TABLE "RemoteVolume_Temp" (
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

INSERT INTO "RemoteVolume_Temp" SELECT "ID", "OperationID", "Name", "Type", "Size", "Hash", "State", "VerificationCount", 0 FROM "RemoteVolume";
DROP TABLE "RemoteVolume";
EXEC sp_rename "RemoteVolume_Temp", "RemoteVolume";

UPDATE "Version" SET "Version" = 3;
