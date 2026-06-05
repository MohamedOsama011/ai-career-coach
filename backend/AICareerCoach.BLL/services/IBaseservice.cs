using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.Services
{
    public interface IBaseservice<T> where T : class
    {
		//here we can change arguments type 
		public List<T>? Getall();
		public T? GetbyId(int id);
		public void Add(T item);
		public void Delete(T item);
		public void Update(T item);
	}
}
