using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AICareerCoach.DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AICareerCoach.DAL.Configurations
{
    public  class Configuration
    {
        public class UserConfiguration:IEntityTypeConfiguration<User>
        {
            public void Configure(EntityTypeBuilder<User>builder)
            {
                builder.HasKey(x => x.Id);
            }
        }
    }
}
