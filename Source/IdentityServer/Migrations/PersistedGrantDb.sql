CREATE TABLE IF NOT EXISTS `__EFMigrationsHistory` (
    `MigrationId` varchar(150) CHARACTER SET utf8mb4 NOT NULL,
    `ProductVersion` varchar(32) CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK___EFMigrationsHistory` PRIMARY KEY (`MigrationId`)
) CHARACTER SET=utf8mb4;

START TRANSACTION;

ALTER DATABASE CHARACTER SET utf8mb4;

CREATE TABLE `DeviceCodes` (
    `UserCode` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `DeviceCode` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `SubjectId` varchar(200) CHARACTER SET utf8mb4 NULL,
    `SessionId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ClientId` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(200) CHARACTER SET utf8mb4 NULL,
    `CreationTime` datetime(6) NOT NULL,
    `Expiration` datetime(6) NOT NULL,
    `Data` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_DeviceCodes` PRIMARY KEY (`UserCode`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `Keys` (
    `Id` varchar(255) CHARACTER SET utf8mb4 NOT NULL,
    `Version` int NOT NULL,
    `Created` datetime(6) NOT NULL,
    `Use` varchar(255) CHARACTER SET utf8mb4 NULL,
    `Algorithm` varchar(100) CHARACTER SET utf8mb4 NOT NULL,
    `IsX509Certificate` tinyint(1) NOT NULL,
    `DataProtected` tinyint(1) NOT NULL,
    `Data` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_Keys` PRIMARY KEY (`Id`)
) CHARACTER SET=utf8mb4;

CREATE TABLE `PersistedGrants` (
    `Key` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Type` varchar(50) CHARACTER SET utf8mb4 NOT NULL,
    `SubjectId` varchar(200) CHARACTER SET utf8mb4 NULL,
    `SessionId` varchar(100) CHARACTER SET utf8mb4 NULL,
    `ClientId` varchar(200) CHARACTER SET utf8mb4 NOT NULL,
    `Description` varchar(200) CHARACTER SET utf8mb4 NULL,
    `CreationTime` datetime(6) NOT NULL,
    `Expiration` datetime(6) NULL,
    `ConsumedTime` datetime(6) NULL,
    `Data` longtext CHARACTER SET utf8mb4 NOT NULL,
    CONSTRAINT `PK_PersistedGrants` PRIMARY KEY (`Key`)
) CHARACTER SET=utf8mb4;

CREATE UNIQUE INDEX `IX_DeviceCodes_DeviceCode` ON `DeviceCodes` (`DeviceCode`);

CREATE INDEX `IX_DeviceCodes_Expiration` ON `DeviceCodes` (`Expiration`);

CREATE INDEX `IX_Keys_Use` ON `Keys` (`Use`);

CREATE INDEX `IX_PersistedGrants_ConsumedTime` ON `PersistedGrants` (`ConsumedTime`);

CREATE INDEX `IX_PersistedGrants_Expiration` ON `PersistedGrants` (`Expiration`);

CREATE INDEX `IX_PersistedGrants_SubjectId_ClientId_Type` ON `PersistedGrants` (`SubjectId`, `ClientId`, `Type`);

CREATE INDEX `IX_PersistedGrants_SubjectId_SessionId_Type` ON `PersistedGrants` (`SubjectId`, `SessionId`, `Type`);

INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`)
VALUES ('20220201010441_Grants', '6.0.1');

COMMIT;

