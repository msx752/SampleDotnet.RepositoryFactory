﻿namespace SampleDotnet.RepositoryFactory.Tests.Cases.Infrastructure.DbContexts.DbContextModels.Entities
{
    [Table("TestUserEntity")]
    public class TestDisposeEntity : IHasDateTimeOffset
    {
        /// <summary>
        /// SELF NOTE: Use GUID for the PrimaryKey and SecondaryKey to be able to fix ID Conflict Exceptions when commiting the changes
        /// </summary>
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Name { get; set; }

        [Required(AllowEmptyStrings = false)]
        public string Surname { get; set; }

        public DateTimeOffset? CreatedAt { get; set; }
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}