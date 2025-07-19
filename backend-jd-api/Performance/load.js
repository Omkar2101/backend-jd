import http from "k6/http";
import { check, sleep } from "k6";

export let options = {
  stages: [
    { duration: "30s", target: 25 },  // Ramp up to 25 users in 30s
    { duration: "30s", target: 50 },  // Increase to 50 users in next 30s
    { duration: "30s", target: 100 }, // Then up to 100 users
    { duration: "1m", target: 100 },  // Hold at 100 users for 1 minute
    { duration: "30s", target: 0 },   // Ramp down to 0 users
  ],
};

const baseUrl = "http://localhost:5268/api/jobs"; 

export default function () {
  // 1. GET all jobs with pagination
  let allJobsRes = http.get(`${baseUrl}?skip=0&limit=5`);
  check(allJobsRes, {
    "GET all jobs: status 200": (r) => r.status === 200,
  });

  sleep(1);

  // 2. GET job by known ID (optional — replace with real test ID)
  const testJobId = "686faf65a06ca8bed87d201b"; 
  if (testJobId) {
    let jobByIdRes = http.get(`${baseUrl}/${testJobId}`);
    check(jobByIdRes, {
      " GET job by ID: status 200 or 404": (r) =>
        r.status === 200 || r.status === 404,
    });
  }

  sleep(1);

  // 3. GET jobs by known user email
  const testEmail = "rathod@gmail.com"; // Replace with a valid test email
  let userJobsRes = http.get(`${baseUrl}/user/${encodeURIComponent(testEmail)}`);
  check(userJobsRes, {
    " GET user jobs: status 200 or 404": (r) =>
      r.status === 200 || r.status === 404,
  });

  sleep(1);
}
