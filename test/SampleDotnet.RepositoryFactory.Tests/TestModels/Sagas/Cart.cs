using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{    
    // Cart entity
    public class Cart
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        public Guid TransactionId { get; set; }
        public List<CartItem> Items { get; set; }
        public CartStatus Status { get; set; }
    }
}
