# LinkedIn Contact Gather

This program leverages LinkedIn Sales Navigator to obtain contacts from a target company.

The program will obtain details for each employee, such as first name, last name, and title, and save those results to an sqllite database.

## Requirements
python3
LinkedIn Sales Navigator Account (30-day trials available)
Valid session information from LinkedIn Sales Navigator Account
LinkedIn Profile URL of an employee who works at the target company

## Usage
Log into LinkedIn Sales Navigator. Use your browser's development tools to find the relevant cookie values.

Update the headers.txt and cookies.txt files to contain your current cookie values.

Update contactgather.py to contain the LinkedIn profile URL of an employee who works at the target company. The current placeholder is my dear coworker Beau Bullock =).

Run the program with the following command:

```
python3 contactgather.py
```

Select the target company from the list of options.

## Why LinkedIn Sales Navigator?

Trying to crawl contacts with a normal account is cumbersome. LinkedIn very frequently updates the HTML tags on its normal site. This makes it difficult to keep the program updated so that it parses the pages correctly. On top of that, LinkedIn will quickly detect that you are up to no good and will block your account.

The LinkedIn sanctioned API is extremely restrictive. Basically, you can parse the information from your profile. That is pretty much it.

## So...is this legal?
It sure is! In 2019, a court court unanimously ruled that it was not illegal to "scrape" information from LinkedIn. The following are some of the main points of the ruling:
- Since members voluntarily provide their information to be published on LinkedIn, there is no expectation of privacy of that information
- Since the data on the LinkedIn site is available to the general public, it is not a violation of the Computer Fraud and Abuse Act (CFAA) to automate the collection of the data from LinkedIn’s site
- Violating the Terms of Service (ToS) of a website does not necessarily mean that the CFAA has been violated

Reference: Woollacott,E. (2019). LinkedIn Data Scraping Ruled Legal. Retrieved from https://www.forbes.com/sites/emmawoollacott/2019/09/10/linkedin-
data-scraping-ruled-legal/#5fd64fbb1b54

## Are there other tools that do this?
I did not find any that were freely and publicly available that specifically leverage LinkedIn Sales Navigator