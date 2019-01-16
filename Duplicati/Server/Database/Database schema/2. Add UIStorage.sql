/*
Key/value storage for frontends
*/
CREATE TABLE "UIStorage" (
    "Scheme" NVARCHAR(450) NOT NULL, 
    "Key" NVARCHAR(450) NOT NULL, 
    "Value" TEXT NOT NULL
);

