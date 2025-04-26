# Answers to Technical Questions

## 1. How long did you spend on the coding assignment? What would you add to your solution if you had more time?

I spent approximately 9 hours on the coding assignment. If I had more time, I would improve the architecture of the solution and make the input and output currencies dynamic, allowing the user to specify them.

## 2. What was the most useful feature that was added to the latest version of your language of choice? Please include a snippet of code that shows how you've used it.

I really like the Collection Expressions introduced in C# 12; they simplify the work significantly.  
Example:  
```csharp
var response = await mockService.Object.GetPricesAsync(["BTC", "ETH"]);
```

## 3. How would you track down a performance issue in production? Have you ever had to do this?

I first try to identify the source of the issue by testing and reviewing logs. Then, if necessary, I use tools like Visual Studio or other profiling tools to analyze the problematic section of the code. After that, I start working on resolving the issue.

## 4. What was the latest technical book you have read or tech conference you have been to? What did you learn?

The last conference I attended was a webinar hosted by Amin Mesbahi titled "Review of Required Skills and the Path to Becoming a Senior Software Engineer." During this conference, I took notes to better study the following topics:

- How to write User Story, Acceptance Criteria
- Clarification Techniques
- Technical Documentation Basics
- Task Analysis (for juniors)
- System Analysis
- User Story Mapping
- Dependency Analysis
- Prioritization Techniques (like Kano Model, MoSCoW)
- Stakeholder Interviewing Techniques
- Expectation Management

## 5. What do you think about this technical assessment?

I think the assessment was well-designed, but my preference for this type of task is to have discussions with the developer about their practical and technical knowledge. Nowadays, with the ease of writing code using LLMs, I personally used one to improve my coding in this project. In my opinion, what matters more is the developer's problem-solving skills.

## 6. Please, describe yourself using JSON.

```json
{
  "firstName": "Armin",
  "lastName": "Sharifi",
  "occupation": "Software Engineer",
  "age": 22,
  "city": "Tehran",
  "interests": [
    "Studying software architecture",
    "Gaming",
    "Hanging out with friends"
  ]
}
```
