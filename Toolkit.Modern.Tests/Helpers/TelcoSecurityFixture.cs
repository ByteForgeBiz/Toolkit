using ByteForge.Toolkit.Data;
using ByteForge.Toolkit.Security.Rbac;

namespace ByteForge.Toolkit.Tests.Helpers;

/// <summary>Minimal shared identity schema in a private disposable database, with no application data or mail.</summary>
internal sealed class TelcoSecurityFixture : IDisposable
{
    private readonly DisposableSqlDatabase database = new();
    public const string Password = "Fixture-password-123!";
    public DBAccess Db { get; }
    public TelcoRbacStore One { get; }
    public TelcoRbacStore Two { get; }

    public TelcoSecurityFixture()
    {
        Db = database.CreateAccess();
        One = new TelcoRbacStore(Db, "One");
        Two = new TelcoRbacStore(Db, "Two");
        try
        {
            database.Execute(@"
CREATE TABLE tbl_TELCO_Projects(Id INT PRIMARY KEY, ProjectKey VARCHAR(50) UNIQUE, IsActive BIT);
CREATE TABLE tbl_TELCO_Users(Id INT IDENTITY PRIMARY KEY, Username VARCHAR(100) UNIQUE, DisplayName NVARCHAR(200), Email VARCHAR(254) UNIQUE,
 IsActive BIT, IsSuperAdmin BIT, LastLoginAtUtc DATETIME NULL, CreatedAtUtc DATETIME NULL, UpdatedAtUtc DATETIME NULL,
 PasswordSalt VARCHAR(500), PasswordHash VARCHAR(500), PasswordIterations INT, LastPasswordChangedAtUtc DATETIME NULL,
 PasswordResetRequestedAtUtc DATETIME NULL);
CREATE TABLE tbl_TELCO_UserProjects(UserId INT REFERENCES tbl_TELCO_Users(Id), ProjectId INT REFERENCES tbl_TELCO_Projects(Id),
 IsActive BIT, CreatedAtUtc DATETIME NULL, UpdatedAtUtc DATETIME NULL, PRIMARY KEY(UserId,ProjectId));
CREATE TABLE tbl_TELCO_Roles(Id INT IDENTITY PRIMARY KEY, ProjectId INT REFERENCES tbl_TELCO_Projects(Id), Name VARCHAR(100), DisplayName VARCHAR(200),
 Description VARCHAR(1000), LandingController VARCHAR(100) NOT NULL, Priority INT, IsSystem BIT, IsActive BIT,
 CreatedAtUtc DATETIME NULL, UpdatedAtUtc DATETIME NULL);
CREATE TABLE tbl_TELCO_Privileges(Id INT IDENTITY PRIMARY KEY, Name VARCHAR(100) UNIQUE, DisplayName VARCHAR(200), Description VARCHAR(1000), Category VARCHAR(100), IsActive BIT);
CREATE TABLE tbl_TELCO_ProjectPrivileges(ProjectId INT, PrivilegeId INT, CreatedAtUtc DATETIME NULL, PRIMARY KEY(ProjectId,PrivilegeId));
CREATE TABLE tbl_TELCO_UserRoles(UserId INT, RoleId INT, CreatedAtUtc DATETIME NULL, PRIMARY KEY(UserId,RoleId));
CREATE TABLE tbl_TELCO_RolePrivileges(RoleId INT, PrivilegeId INT, CreatedAtUtc DATETIME NULL, PRIMARY KEY(RoleId,PrivilegeId));
CREATE TABLE tbl_TELCO_UserPasswordResetTokens(Id INT IDENTITY PRIMARY KEY, UserId INT REFERENCES tbl_TELCO_Users(Id),
 TokenHash VARCHAR(64) UNIQUE, RequestIp VARCHAR(100), RequestedAtUtc DATETIME, ExpiresAtUtc DATETIME, ConsumedAtUtc DATETIME NULL, IsInvitation BIT);
INSERT INTO tbl_TELCO_Projects VALUES(1,'One',1),(2,'Two',1);
INSERT INTO tbl_TELCO_Privileges(Name,IsActive) VALUES('Reports',1),('Finance',1),('Rbac.Delegate',1);
INSERT INTO tbl_TELCO_ProjectPrivileges(ProjectId,PrivilegeId) VALUES(1,1),(1,2),(1,3),(2,2);
INSERT INTO tbl_TELCO_Roles(ProjectId,Name,DisplayName,LandingController,Priority,IsSystem,IsActive)
 VALUES(1,'Admin','Admin','Finance',1000,1,1),(1,'Reader','Reader','Reports',100,0,1),
 (1,'Finance','Finance','Finance',100,0,1),(1,'Operators','Operators','Reports',100,0,1),
 (2,'ForeignRole','ForeignRole','Finance',100,0,1),(1,'ClientAdmin','Client Admin','',0,1,1);
INSERT INTO tbl_TELCO_RolePrivileges(RoleId,PrivilegeId) VALUES(2,1),(3,2),(4,3),(5,2);
");
            // A lower work factor keeps fixture seeding quick; production APIs still create their default hashes.
            var hash = new TelcoPasswordHashService().CreateHash(Password, 1000);
            foreach (var user in new[] { "Super", "Delegate", "Member", "Foreign", "Admin", "NameOnly" })
                Execute("INSERT INTO tbl_TELCO_Users(Username,DisplayName,Email,IsActive,IsSuperAdmin,PasswordSalt,PasswordHash,PasswordIterations,LastPasswordChangedAtUtc) " +
                    "VALUES(@Name,@Display,@Email,1,@Super,@Salt,@Hash,@Iterations,GETUTCDATE())",
                    user, user, user + "@example.invalid", user == "Super", hash.Salt, hash.Hash, hash.Iterations);
            database.Execute(@"
INSERT INTO tbl_TELCO_UserProjects(UserId,ProjectId,IsActive) VALUES(2,1,1),(3,1,1),(3,2,1),(4,2,1),(5,1,1),(6,1,1);
INSERT INTO tbl_TELCO_UserRoles(UserId,RoleId) VALUES(2,2),(2,4),(3,2),(3,5),(4,5),(5,1),(6,6);
");
        }
        catch { database.Dispose(); throw; }
    }

    public void Execute(string sql, params object[] values)
    {
        Assert.IsTrue(Db.ExecuteQuery(sql, values), Db.LastException?.Message);
    }

    public void ExecuteDdl(string sql) => database.Execute(sql);
    public TelcoIdentityStore Identity(string project) => new(database.CreateAccess(), project);
    public TelcoRbacStore Store(string project) => new(database.CreateAccess(), project);
    public void Dispose() => database.Dispose();
}
