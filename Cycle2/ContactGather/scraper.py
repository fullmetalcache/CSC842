'''
file: scraper.py
author: Brian Fehrman
date: 2021-05-26
description:
    Handles retrieving employee information from LinkedIn Sales Navigator and
    stores it in an sqllite database.
    NOTE: This requires that you populate headers.txt and cookies.txt with
        valid session information. This can be obtained by logging into LinkedIn 
        Sales Navigator and then using your browser's development tools to obtain 
        the cookie values.
'''

from datetime import date
from ContactGather.employee import Employee
from ContactGather.printColors import bcolors
import html
import json
from os import path
import requests
import sqlite3
import sys

class Scraper():

    companyId = ""
    companyName = ""
    dbName = "ealDb.db"
    employeeList = {}
    headersFile = "headers.txt"
    headers = ""
    initialRun = False
    password = ""
    cookiesFile = "cookies.txt"
    cookies = ""
    username = ""
    userUrl = ""

    def __init__(self, userUrl):
        self.userUrl = userUrl

        if not path.exists(self.dbName):
            self.createDatabase()

        if(self.checkAuth()):
            print(
                f"{bcolors.OKGREEN}Session cookies appear to be valid...proceeding...{bcolors.ENDC}")
        else:
            print(
                f"{bcolors.WARNING}Session cookies invalid...exiting...{bcolors.ENDC}")

        self.getCompanyId()

    def checkAuth(self):
        '''
        checkAuth function will test to see if the provided cookies and header values are still valid.
        This check is performed by requesting an authenticated LinkedIn SalesNavigator link and
        checking that the response is as expected.
        '''
        # Use search for BHIS as test if auth cookies are still valid. Will find a different way later...
        testUrl = f"https://www.linkedin.com/sales/search/people/list/employees-for-account/3569774"
        valid = False

        try:

            # Read saved files and convert back to dictionary format
            self.headers = json.load(open(self.headersFile))
            cookiesTemp = json.load(open(self.cookiesFile))

            # set the session values
            sess = requests.session()
            for cookie in cookiesTemp:
                sess.cookies.set(name=cookie, value=cookiesTemp[cookie])

            self.cookies = sess.cookies

            # make the request and check that it contains the expected value
            resp = requests.get(testUrl, headers=self.headers,
                                cookies=self.cookies, allow_redirects=False)

            if "elements" in resp.text:
                valid = True
        except:
            pass

        return valid

    def createDatabase(self):
        '''
        createDatabase will create a new sqllite database for use by the program
        '''
        conn = sqlite3.connect(self.dbName)
        c = conn.cursor()

        # Create table
        c.execute('''CREATE TABLE info
                   (companyId text, dateAdded text, firstName text, fullName text, lastName text, memberId text, title text)''')

        # Save the changes
        conn.commit()

        # Close the connection
        conn.close()

    def getEmployees(self):
        '''
        getEmployees will get a list of all employees for the given company.
        It will parse various attributes for each employee and then save or
        update the results in the sqllite database
        '''

        # get information needed for proper paging of results
        employeesPerPage = int(25)
        numEmployees = int(self.getNumberEmployees())
        numPages = numEmployees / employeesPerPage

        if 0 < (numEmployees % employeesPerPage):
            numPages += 1

        currPage = 1


        # iterate through the pages of results
        while currPage <= numPages:
            employeeUrl = f"https://www.linkedin.com/sales/search/people/list/employees-for-account/{self.companyId}?&page={currPage}"

            getResp = requests.get(
                employeeUrl, headers=self.headers, cookies=self.cookies, allow_redirects=False)

            parseResp = html.unescape(getResp.text)

            # tokenize the data on the page so that that infor for each employee is part of a token.
            # yea, I know there are libraries to parse HTML and XML and all of that but I feel
            # that the effort taken to troubleshoot that is not worth it to make the code a bit prettier...
            # especially when it is basically doing this same thing underneath of the hood
            employeeData = parseResp.split("elements", 1)[1].split("</code>", 1)[0]
            employeeTokens = employeeData.split('lastName":"')

            # iterate through the tokens and extract the information for each employee
            for token in employeeTokens[1:]:
                currEmp = Employee()
                currEmp.companyId = self.companyId
                try:
                    currEmp.lastName = token.split('"', 1)[0]
                except:
                    currEmp.lastName = "NULL"

                try:
                    currEmp.firstName = token.split('firstName":"', 1)[1].split('"', 1)[0]
                except:
                    currEmp.firstName = "NULL"

                try:
                    currEmp.title = token.split('title":"', 1)[1].split('"', 1)[0]
                except:
                    currEmp.title = "NULL"

                try:
                    currEmp.memberId = token.split('urn:li:member:', 1)[1].split('"', 1)[0]
                except:
                    currEmp.memberId = "NULL"

                try:
                    currEmp.fullName = token.split('fullName":"', 1)[1].split('"', 1)[0]
                except:
                    currEmp.fullName = "NULL"

                self.employeeList[currEmp.memberId] = currEmp

            currPage += 1

        conn = sqlite3.connect(self.dbName)
        c = conn.cursor()

        # write out the results for each employee
        for emp in self.employeeList:
            # Check if employee exists in DB and insert or update accordingly
            memberId = self.employeeList[emp].memberId
            c.execute("SELECT * FROM info WHERE memberId = ?", (memberId))
            exists = c.fetchone()

            # The queries below use parameter substition...just in case anybody
            # wants to get clever and try to cause an SQL Injection with their LinkedIn
            # profile information
            if (exists is None) or (exists == "None"):
                self.employeeList[emp].dateAdded = date.today()
                c.execute(
                    "INSERT INTO info (companyId, dateAdded, firstName, fullName, lastName, memberId, title) values (?, ?, ?, ?, ?, ?, ?)", (self.employeeList[emp].companyId, self.employeeList[emp].dateAdded, self.employeeList[emp].firstName, self.employeeList[emp].fullName, self.employeeList[emp].lastName, self.employeeList[emp].memberId, self.employeeList[emp].title))

            else:
                c.execute(
                    "UPDATE info SET firstName=?, fullName=?, lastName=?, title=? WHERE memberId = ?", (self.employeeList[emp].firstName, self.employeeList[emp].fullName, self.employeeList[emp].lastName, self.employeeList[emp].title, self.employeeList[emp].memberId))

        conn.commit()
        conn.close()

    def getNumberEmployees(self):
        '''
        getNumberEmployees will determine the total number of employees for the given company.
        This information is needed for proper paging of the employees in the search results.
        '''
        companyUrl = f"https://www.linkedin.com/sales/search/people/list/employees-for-account/{self.companyId}"

        print(companyUrl)

        # get the relevant page and parse the results
        resp = requests.get(companyUrl, headers=self.headers,
                            cookies=self.cookies, allow_redirects=False)
        parseResp = html.unescape(resp.text)

        numEmployees = parseResp.split('"paging":{"total":')[1].split(",")[0]

        return numEmployees

    def getCompanyId(self):
        '''
        getCompanyId will get a list of potential company IDs based upon the companies
        that are listed on a target employee's LinkedIn page. The user is then presented
        with a list of possibilities for which company they are targeting.
        '''

        # get the relevant page and tokenize the list of potential companies
        testResp = requests.get(
            self.userUrl, headers=self.headers, cookies=self.cookies, allow_redirects=False)
        parseResp = html.unescape(testResp.text)
        companiesParse = parseResp.split('dateRange":{"start":{"month"')
        possibleCompanies = {}

        # parse the list of potential companies and retrieve the company ID and company Name
        # for each of them
        for res in companiesParse:
            if '"end":{' not in res:
                try:
                    companyId = res.split("urn:li:fsd_company:", 1)[1].split('"')[0]
                    companyName = res.split('companyName":"', 1)[1].split('"', 1)[0]

                    possibleCompanies[companyName] = companyId
                except:
                    continue

        # Present the user with a list of results and perform error checking
        while True:
            choice = 0
            print("\n")
            for idx, key in enumerate(possibleCompanies):
                print(
                    f"{bcolors.OKCYAN}[{idx}] : {key} {possibleCompanies[key]}{bcolors.ENDC}")

            try:
                choice = input(
                    f"{bcolors.OKBLUE}\nPlease choose current company: {bcolors.ENDC}")

            except KeyboardInterrupt:
                print(
                    f"{bcolors.OKBLUE}\n\nI see you've changed your mind, good bye!\n\n{bcolors.ENDC}")
                sys.exit()

            try:
                choice = int(choice)
            except ValueError:
                print(
                    f"{bcolors.WARNING}Invalid choice, please choose again\n{bcolors.ENDC}")
                continue

            if choice >= len(list(possibleCompanies)):
                print(
                    f"{bcolors.WARNING}Invalid choice, please choose again\n{bcolors.ENDC}")
                continue

            self.companyName = list(possibleCompanies)[choice]
            self.companyId = possibleCompanies[companyName]

            break
