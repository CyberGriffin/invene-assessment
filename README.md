# invene-assessment

# How to Build and Run
1. **Requirements**:
    - .NET SDK 6.0 or later
2. **Clone the Repository**:
    ```bash
    git clone https://github.com/CyberGriffin/invene-assessment.git
    ```
3. **Configure Settings**:
    Update the appsettings.json file with the allowed PHI keys and deny value. Example:
   ```json
   {
     "PHI": {
       "Allowed": ["Order Details", "Email"],
       "DenyValue": "REDACTED"
     }
   }
   ```
5. **Run the Application**:
   ```bash
   dotnet watch --project InveneWebApp/InveneWebApp.csproj
   ```
6. **Access the Application**: The app will automatically open. If not, check the run logs for the port information.

# Approach
My approach was to keep track of allowed keywords specified via a config file and redact values that had unallowed keys.
I did this to ensure PHI elements were redacted by default, and that only explicitly defined elements were kept.

#  Assumptions:
* **File Format**: All input files are filled with key-value pairs or key-list pairs.
* **Span**: Values do not span multiple lines.

# Areas for Improvement
* **User Interface**: The user interface is very basic, and provides no details to the user.
* **Processing**: Move the logic for processing and redacting the files to it's own service.
* **Testing**: Implement unit tests to increase maintainability.
* **Regex**: The robustness of this solution could be improved by using regex to find patterns in the PHI, allowing for more flexibility in the input files.