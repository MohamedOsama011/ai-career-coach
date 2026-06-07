using AICareerCoach.API.Response;
using AICareerCoach.BLL.DTO.User;
using AICareerCoach.BLL.Services;
using AICareerCoach.DAL.Entities;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AICareerCoach.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IBaseservice<User> _userService;


        public UserController(IBaseservice<User> baseservice)
        {
            _userService = baseservice;
        }

        //get all
        [HttpGet]
        public GeneralResponse Getall()
        {
            List<User> users = _userService.Getall();
            GeneralResponse response = new GeneralResponse();
            if(users!=null)
            {
                Add user = new Add();
                foreach (var item in users)
                {
                    user.Name=item.FullName;
                    user.email = item.Email;
                }
                response.Sucess=true;
                response.Data=user;
            }
            else
            {
                response.Sucess = false;
                response.Data = "there is no users untill now";
            }
            return response;
        }
        //get one 
        [HttpGet("{id:int}")]
        public GeneralResponse GetUser(int id)
        {
            GeneralResponse response=new GeneralResponse();
           var user =_userService.GetbyId(id);
            if(user!=null)
            {
                Get user1= new Get();
                user1.name=user.FullName;
                user1.email=user.Email;
                user1.title = user.CareerGoal;
                response.Sucess=true;
                response.Data=user1;
               
            }
            else
            {
                response.Sucess=false;
                response.Data = "not found";

            }
            return response;
        }
       //add user
        [HttpPost]
        public IActionResult AddUser([FromBody]Add user)
        {
            var user1 = new User();
            user1.FullName = user.Name;
            user1.Email = user.email;
            _userService.Add(user1);
            return CreatedAtAction("GetUser ", new{ id = user1.Id }, user1);
            //return Created("created successfully", user1);
        }
        //delete 
        [HttpDelete("{id}")]
        public GeneralResponse    DeleteUser(int id)
        {
            GeneralResponse generalResponse=new GeneralResponse();
            var user = _userService.GetbyId(id);
            if (user != null)
            {
                _userService.Delete(user);
                generalResponse.Sucess = true;
                generalResponse.Data = "deleted sucessfully";
            }
            else
            { 
                generalResponse.Sucess = false;
                generalResponse.Data = "not found";//should be handeled more
            }
            return generalResponse;
        }
        //update
        [HttpPut("{id:int}")]
        public GeneralResponse Edit ([FromBody] Update user1,int id)
        {
            var generalResponse=new GeneralResponse();
            var user= _userService.GetbyId(id);
            if(user!=null)
            {
                user.Email= user1.Email;
                user.FullName=user1.FullName;
                _userService.Update(user);
                generalResponse.Sucess = true;
                generalResponse.Data = "user updated successfly";
            }
            else
            {
                generalResponse.Sucess = false;
                generalResponse.Data = "user not fouond";
			}
            return generalResponse;
		}

    }
}