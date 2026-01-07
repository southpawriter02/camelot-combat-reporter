using CamelotCombatReporter.Core.CharacterBuilding.Models;
using CamelotCombatReporter.Core.CharacterBuilding.Services;
using CamelotCombatReporter.Core.Models;

namespace CamelotCombatReporter.Core.Tests.CharacterBuilding;

public class CharacterProfileServiceTests : IDisposable
{
    private readonly string _testProfilesDirectory;
    private readonly CharacterProfileService _service;

    public CharacterProfileServiceTests()
    {
        _testProfilesDirectory = Path.Combine(Path.GetTempPath(), $"CCR_Tests_{Guid.NewGuid()}");
        _service = new CharacterProfileService(_testProfilesDirectory);
    }

    public void Dispose()
    {
        _service.Dispose();
        if (Directory.Exists(_testProfilesDirectory))
        {
            Directory.Delete(_testProfilesDirectory, recursive: true);
        }
    }

    private CharacterProfile CreateTestProfile(string name = "TestChar", Realm realm = Realm.Albion, CharacterClass charClass = CharacterClass.Armsman)
    {
        return new CharacterProfile
        {
            Name = name,
            Realm = realm,
            Class = charClass,
            Level = 50
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Profile CRUD Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProfile_ValidInput_ReturnsProfile()
    {
        // Arrange
        var profile = CreateTestProfile("TestWarrior");

        // Act
        var created = await _service.CreateProfileAsync(profile);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("TestWarrior", created.Name);
        Assert.Equal(Realm.Albion, created.Realm);
        Assert.Equal(CharacterClass.Armsman, created.Class);
    }

    [Fact]
    public async Task CreateProfile_ReturnsNewGuid()
    {
        // Arrange
        var profile = CreateTestProfile();

        // Act
        var created = await _service.CreateProfileAsync(profile);

        // Assert
        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.NotEqual(profile.Id, created.Id);
    }

    [Fact]
    public async Task GetProfile_ExistingId_ReturnsProfile()
    {
        // Arrange
        var profile = CreateTestProfile("MyArmsman");
        var created = await _service.CreateProfileAsync(profile);

        // Act
        var retrieved = await _service.GetProfileAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(created.Id, retrieved.Id);
        Assert.Equal("MyArmsman", retrieved.Name);
    }

    [Fact]
    public async Task GetProfile_NonExistingId_ReturnsNull()
    {
        // Act
        var retrieved = await _service.GetProfileAsync(Guid.NewGuid());

        // Assert
        Assert.Null(retrieved);
    }

    [Fact]
    public async Task GetAllProfiles_ReturnsAllCreated()
    {
        // Arrange
        await _service.CreateProfileAsync(CreateTestProfile("Char1"));
        await _service.CreateProfileAsync(CreateTestProfile("Char2"));
        await _service.CreateProfileAsync(CreateTestProfile("Char3"));

        // Act
        var profiles = await _service.GetAllProfilesAsync();

        // Assert
        Assert.Equal(3, profiles.Count);
    }

    [Fact]
    public async Task GetProfilesByRealm_FiltersCorrectly()
    {
        // Arrange
        await _service.CreateProfileAsync(CreateTestProfile("AlbChar1", Realm.Albion, CharacterClass.Armsman));
        await _service.CreateProfileAsync(CreateTestProfile("AlbChar2", Realm.Albion, CharacterClass.Paladin));
        await _service.CreateProfileAsync(CreateTestProfile("MidChar1", Realm.Midgard, CharacterClass.Warrior));

        // Act
        var albionProfiles = await _service.GetProfilesByRealmAsync(Realm.Albion);
        var midgardProfiles = await _service.GetProfilesByRealmAsync(Realm.Midgard);

        // Assert
        Assert.Equal(2, albionProfiles.Count);
        Assert.Single(midgardProfiles);
    }

    [Fact]
    public async Task UpdateProfile_ValidChange_Persists()
    {
        // Arrange
        var profile = CreateTestProfile("OriginalName");
        var created = await _service.CreateProfileAsync(profile);
        var updated = created with { Name = "NewName", Level = 45 };

        // Act
        await _service.UpdateProfileAsync(updated);
        var retrieved = await _service.GetProfileAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("NewName", retrieved.Name);
        Assert.Equal(45, retrieved.Level);
    }

    [Fact]
    public async Task DeleteProfile_ExistingId_Removes()
    {
        // Arrange
        var profile = CreateTestProfile("ToBeDeleted");
        var created = await _service.CreateProfileAsync(profile);

        // Act
        var deleted = await _service.DeleteProfileAsync(created.Id);
        var retrieved = await _service.GetProfileAsync(created.Id);

        // Assert
        Assert.True(deleted);
        Assert.Null(retrieved);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Session Attachment Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task AttachSession_ValidIds_LinksSession()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile());
        var sessionId = Guid.NewGuid();

        // Act
        await _service.AttachSessionAsync(profile.Id, sessionId);
        var attachedIds = await _service.GetAttachedSessionIdsAsync(profile.Id);

        // Assert
        Assert.Single(attachedIds);
        Assert.Equal(sessionId, attachedIds[0]);
    }

    [Fact]
    public async Task DetachSession_ValidIds_UnlinksSession()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile());
        var sessionId = Guid.NewGuid();
        await _service.AttachSessionAsync(profile.Id, sessionId);

        // Act
        await _service.DetachSessionAsync(profile.Id, sessionId);
        var attachedIds = await _service.GetAttachedSessionIdsAsync(profile.Id);

        // Assert
        Assert.Empty(attachedIds);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build Management Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateBuild_ValidInput_AddsToBuildHistory()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile());
        var build = new CharacterBuild { Name = "RR5 Nuke Build", RealmRank = 5 };

        // Act
        var created = await _service.CreateBuildAsync(profile.Id, build);
        var history = await _service.GetBuildHistoryAsync(profile.Id);

        // Assert
        Assert.NotNull(created);
        Assert.Equal("RR5 Nuke Build", created.Name);
        Assert.Single(history);
    }

    [Fact]
    public async Task SetActiveBuild_ValidId_SetsActive()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile());
        var build1 = await _service.CreateBuildAsync(profile.Id, new CharacterBuild { Name = "Build 1" });
        var build2 = await _service.CreateBuildAsync(profile.Id, new CharacterBuild { Name = "Build 2" });

        // Act
        await _service.SetActiveBuildAsync(profile.Id, build1.Id);
        var retrieved = await _service.GetProfileAsync(profile.Id);

        // Assert
        Assert.NotNull(retrieved?.ActiveBuild);
        Assert.Equal(build1.Id, retrieved.ActiveBuild.Id);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Import/Export Tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExportProfile_ValidId_ReturnsJson()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile("ExportTest"));

        // Act
        var json = await _service.ExportProfileAsync(profile.Id);

        // Assert
        Assert.Contains("ExportTest", json);
        Assert.Contains("Albion", json);
    }

    [Fact]
    public async Task ImportProfile_ValidJson_CreatesNewProfile()
    {
        // Arrange
        var original = await _service.CreateProfileAsync(CreateTestProfile("ImportTest"));
        var json = await _service.ExportProfileAsync(original.Id);

        // Act
        var imported = await _service.ImportProfileAsync(json);

        // Assert
        Assert.NotEqual(original.Id, imported.Id);
        Assert.Equal("ImportTest", imported.Name);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Additional Edge Case Tests (v1.8.2)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateProfile_DuplicateName_AllowsCreation()
    {
        // Arrange - Two profiles can have the same name
        var p1 = await _service.CreateProfileAsync(CreateTestProfile("DupeName"));
        
        // Act
        var p2 = await _service.CreateProfileAsync(CreateTestProfile("DupeName"));

        // Assert
        Assert.NotEqual(p1.Id, p2.Id);
        Assert.Equal(p1.Name, p2.Name);
    }

    [Fact]
    public async Task GetProfile_AfterNewServiceInstance_PersistsData()
    {
        // Arrange
        var created = await _service.CreateProfileAsync(CreateTestProfile("PersistTest"));
        
        // Act - Create new service instance pointing to same directory
        using var newService = new CharacterProfileService(_testProfilesDirectory);
        var retrieved = await newService.GetProfileAsync(created.Id);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal("PersistTest", retrieved.Name);
    }

    [Fact]
    public async Task DeleteProfile_NonExistingId_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteProfileAsync(Guid.NewGuid());

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task AttachSession_DuplicateSessionId_NoDuplicatesInList()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile("DupeSession"));
        var sessionId = Guid.NewGuid();
        
        // Act - Attach same session twice
        await _service.AttachSessionAsync(profile.Id, sessionId);
        await _service.AttachSessionAsync(profile.Id, sessionId);
        var sessions = await _service.GetAttachedSessionIdsAsync(profile.Id);

        // Assert
        Assert.Single(sessions);
    }

    [Fact]
    public async Task CloneBuild_ResetsPerformanceMetrics()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile("CloneTest"));
        var build = new CharacterBuild 
        { 
            Name = "Original",
            PerformanceMetrics = new BuildPerformanceMetrics 
            { 
                TotalDamageDealt = 50000,
                Kills = 25,
                Deaths = 5
            }
        };
        var created = await _service.CreateBuildAsync(profile.Id, build);

        // Act
        var cloned = await _service.CloneBuildAsync(profile.Id, created.Id, "Cloned Build");

        // Assert
        Assert.NotEqual(created.Id, cloned.Id);
        Assert.Equal("Cloned Build", cloned.Name);
        Assert.Null(cloned.PerformanceMetrics);
    }

    [Fact]
    public async Task GetBuildHistory_MultipleBuilds_ContainsAll()
    {
        // Arrange
        var profile = await _service.CreateProfileAsync(CreateTestProfile("HistoryTest"));
        await _service.CreateBuildAsync(profile.Id, new CharacterBuild { Name = "Build 1" });
        await _service.CreateBuildAsync(profile.Id, new CharacterBuild { Name = "Build 2" });
        await _service.CreateBuildAsync(profile.Id, new CharacterBuild { Name = "Build 3" });

        // Act
        var history = await _service.GetBuildHistoryAsync(profile.Id);

        // Assert
        Assert.Equal(3, history.Count);
        Assert.Contains(history, b => b.Name == "Build 1");
        Assert.Contains(history, b => b.Name == "Build 2");
        Assert.Contains(history, b => b.Name == "Build 3");
    }

    [Fact]
    public async Task GetProfileCount_ReturnsCorrectCount()
    {
        // Arrange
        await _service.CreateProfileAsync(CreateTestProfile("Count1"));
        await _service.CreateProfileAsync(CreateTestProfile("Count2"));

        // Act
        var count = await _service.GetProfileCountAsync();

        // Assert
        Assert.Equal(2, count);
    }
}
