namespace InfoBroker.Models
{
    public class CreateCourseResponse
    {
        
        public int  Id { get; set; }
        public string ShortName { get; set; } 
  
    }

    public class CreateCategoryResponse
    {

        public int Id { get; set; }
        public string Name { get; set; }

    }

    public class CreateCourseCategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string IdNumber { get; set; }
        public string Description { get; set; }
        public string Descriptionformat { get; set; }
        public string Parent { get; set; }
        public string Sortorder { get; set; }
        public string Coursecount { get; set; }
        public string Visible { get; set; }
        public string Visibleold { get; set; }
        public string Timemodified { get; set; }
        public string Depth { get; set; }
        public string Theme { get; set; }

        
    }
}