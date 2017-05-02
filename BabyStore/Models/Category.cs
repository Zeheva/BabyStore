using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace BabyStore.Models
{
    public class Category
    {

        public int ID { get; set; }
        [Required(ErrorMessage ="The Name can not be blank")]
        [StringLength(50,MinimumLength =3,ErrorMessage ="Name must be longer then 3 Char")]
        [RegularExpression(@"^[A-Z]+[a-zA-Z''-'\s]*$", ErrorMessage ="Must be letters only")]
        [Display(Name = "Category Name")]
        public string Name { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}