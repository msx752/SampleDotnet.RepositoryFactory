namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    // CartItem entity
    public class CartItem
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public Guid? CartId { get; set; }
    }

}
