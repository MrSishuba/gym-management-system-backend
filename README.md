Gym management system (Booking & Point of Sale System): AV Motion Read Me:
Purpose of project:
Final year Capstone project addressing the business needs of a real-life client through a full Systems Development life Cycle project. The client is a startup gym (AVS Fitness) completely paper based in all areas of business not limited to but including administrative tasks, bookings and sales. The aim of this project was to create a centralised online platform whereby the owner could effectively organize all business operations systematically and for employees and client to categorically interact with the system in performing various operational and routine tasks.

The system comprises of 5 key areas user management, reporting, contract handling, booking and sales. Thus, the key features that characterize this system is the segmentation of users in relation to their respective areas of activity securing users in their profile management, creating autonomy of clients to easily receive information, make bookings and make purchases on the system while employees and the owner(admin) has advanced oversight and means to facilitate operations on the gym system. This project is to be run on local host enviroment

Setting Up the backend:
•	Please go to appsetting.json file and change the connection string name to your local machine name to run a migration of the database at server” Your device name”
•	In Controllers folder under WeeklyNewsController.cs ensure line 14 where there is a file path this will be where you downloaded the backend and then the name of the full path to this same file. In other words, in file explorer where you downloaded this back end go into the project once you will look for the file WeeklyNewsImages file copy this path directly and replace It with the current instance of the path you currently see.
•	Open up the photos app add a new folder called: WeeklyNewsImages 
•	API Keys Request:  please note that ## API Keys for `SendGrid` and `Vonage` API keys are ommitted with placeholders for security purposes this means if the code is run and any functions linked to the two mentioned services are attempted this may result in a crash or will not work please contact the project owner for the api keys and secret then proceed to test and explore the project to its fullest

Setting up initial user i.e Admin:
•	Once swagger opens after running the backend api locate User/Register/Owner and complete initial registration their for User Type and User Status user 1 representing user type Owner and their status on the system is active. From thereon as Owner login into the system and freely use the system. All details on how to use the system are indicated through the information help icons.
Miscellaneous notes:
•	The owner or Admin role is hard coded cannot be dynamically changed unless discarded in the database.
•	The owner will register employees.
•	Sign up on the home screen is for clients; on success an employee will have to process and activate their contract for a member(client) to be activated and their login to work
•	The chatbot will fail to respond if information it is not programmed to process is given there wasn’t enough time spent on it to handle all types of information that could break-it worse off it would require re running the backend for the chatbot.

see youtube link for comprehsive walk through and demonstration of the system: https://www.youtube.com/watch?v=ciJNPRiw78s



