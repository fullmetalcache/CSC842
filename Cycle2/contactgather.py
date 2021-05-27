'''
file: contactgather.py
author: Brian Fehrman
date: 2021-05-26
description:
    Main entry point for gathering contacts from LinkedIn Sales Navigator
'''

from ContactGather.scraper import Scraper

if __name__ == "__main__":

    userUrl = "https://www.linkedin.com/in/beaubullock/"
    linkedInScraper = Scraper(userUrl=userUrl)

    print("Going to get employees...")
    linkedInScraper.getEmployees()

    print("DONE!")
