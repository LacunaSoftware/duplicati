EXEC sp_rename "backup", "_backup_old";

CREATE TABLE "Backup" (
    "ID" INTEGER PRIMARY KEY IDENTITY(1, 1),
    "Name" NVARCHAR(100) NOT NULL,
    "Tags" NVARCHAR(450) NOT NULL,
    "TargetURL" TEXT NOT NULL,
    "DBPath" TEXT NOT NULL
);

INSERT INTO "backup"
  SELECT *
  FROM "_backup_old";

DROP TABLE "_backup_old";
