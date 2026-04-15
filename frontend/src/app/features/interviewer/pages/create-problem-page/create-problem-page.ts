import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { Navbar } from '../../../../shared/components/navbar/navbar';
import { CreateProblemRequest, ProblemListItem } from '../../models/interviewer.models';
import { TestCaseForm } from '../..//pages/test-case-form/test-case-form';
import { InterviewerApi } from '../../services/interviewer-api.service';
import { finalize } from 'rxjs';
@Component({
  selector: 'app-create-problem-page',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, Navbar, TestCaseForm],
  templateUrl: './create-problem-page.html',
  styleUrl: './create-problem-page.scss',
})
export class CreateProblemPage {
  private static readonly stdinExecutionMode = 'stdin';
  private static readonly functionExecutionMode = 'function';
  private static readonly genericFunctionScaffold = {
    csharpStarterCode: `public class Solution
{
    public string Solve(string rawInput)
    {
        
    }
}`,
    pythonStarterCode: `class Solution:
    def solve(self, raw_input: str) -> str:
        
`,
    cppStarterCode: `#include <string>
using namespace std;

class Solution {
public:
    string solve(const string& rawInput) {
        
    }
};`,
    csharpHarnessTemplate: `using System;

{{candidate_code}}

var rawInput = Console.In.ReadToEnd();
var result = new Solution().Solve(rawInput);
Console.WriteLine(result);`,
    pythonHarnessTemplate: `import sys

{{candidate_code}}

raw_input = sys.stdin.read()
result = Solution().solve(raw_input)
print(result)`,
    cppHarnessTemplate: `#include <iostream>
#include <iterator>
#include <string>

{{candidate_code}}

int main() {
    string input(
        (istreambuf_iterator<char>(cin)),
        istreambuf_iterator<char>());

    Solution solution;
    auto result = solution.solve(input);
    cout << result;
    return 0;
}`,
  };
  private static readonly twoSumScaffold = {
    csharpStarterCode: `public class Solution
{
    public int[] TwoSum(int[] nums, int target)
    {
        
    }
}`,
    pythonStarterCode: `from typing import List


class Solution:
    def twoSum(self, nums: List[int], target: int) -> List[int]:
        
`,
    cppStarterCode: `#include <vector>
using namespace std;

class Solution {
public:
    vector<int> twoSum(vector<int>& nums, int target) {
        
    }
};`,
    csharpHarnessTemplate: `using System;
using System.Text.Json;

{{candidate_code}}

var payload = JsonSerializer.Deserialize<TwoSumInput>(Console.In.ReadToEnd());

if (payload is null)
{
    throw new InvalidOperationException("Invalid input.");
}

var result = new Solution().TwoSum(payload.nums ?? Array.Empty<int>(), payload.target);
Console.WriteLine(JsonSerializer.Serialize(result));

public sealed class TwoSumInput
{
    public int[] nums { get; set; } = Array.Empty<int>();
    public int target { get; set; }
}`,
    pythonHarnessTemplate: `import json
import sys
from typing import List

{{candidate_code}}

payload = json.loads(sys.stdin.read() or "{}")
result = Solution().twoSum(payload.get("nums", []), payload.get("target", 0))
print(json.dumps(result))`,
    cppHarnessTemplate: `#include <iostream>
#include <iterator>
#include <sstream>
#include <string>
#include <vector>

{{candidate_code}}

vector<int> ParseNums(const string& input) {
    const auto open = input.find('[');
    const auto close = input.find(']', open == string::npos ? 0 : open);

    if (open == string::npos || close == string::npos || close <= open) {
        return {};
    }

    vector<int> values;
    string content = input.substr(open + 1, close - open - 1);
    string token;
    stringstream stream(content);

    while (getline(stream, token, ',')) {
        if (!token.empty()) {
            values.push_back(stoi(token));
        }
    }

    return values;
}

int ParseTarget(const string& input) {
    const auto targetKey = input.find("\"target\"");
    const auto colon = input.find(':', targetKey == string::npos ? 0 : targetKey);

    if (colon == string::npos) {
        return 0;
    }

    string value = input.substr(colon + 1);
    return stoi(value);
}

string FormatResult(const vector<int>& values) {
    string output = "[";

    for (size_t index = 0; index < values.size(); index++) {
        if (index > 0) {
            output += ",";
        }

        output += to_string(values[index]);
    }

    output += "]";
    return output;
}

int main() {
    string input(
        (istreambuf_iterator<char>(cin)),
        istreambuf_iterator<char>());

    auto nums = ParseNums(input);
    auto target = ParseTarget(input);
    Solution solution;
    auto result = solution.twoSum(nums, target);
    cout << FormatResult(result);
    return 0;
}`,
  };

  private readonly fb = inject(FormBuilder);
  private readonly router = inject(Router);
  private readonly interviewerApi = inject(InterviewerApi);

  loading = false;
  errorMessage = '';
  successMessage = '';
  createdProblem: ProblemListItem | null = null;

  form = this.fb.nonNullable.group({
    title: ['', [Validators.required, Validators.maxLength(200)]],
    description: ['', [Validators.required]],
    difficulty: ['Easy', [Validators.required]],
    topic: ['', [Validators.required, Validators.maxLength(100)]],
    constraintsText: ['', [Validators.required]],
    exampleInput: ['', [Validators.required]],
    exampleOutput: ['', [Validators.required]],
    executionMode: [CreateProblemPage.stdinExecutionMode, [Validators.required]],
    csharpStarterCode: [''],
    pythonStarterCode: [''],
    cppStarterCode: [''],
    csharpHarnessTemplate: [''],
    pythonHarnessTemplate: [''],
    cppHarnessTemplate: [''],
    isPublic: [true, [Validators.required]],
  });

  isFunctionSignatureMode(): boolean {
    return this.form.controls.executionMode.value === CreateProblemPage.functionExecutionMode;
  }

  submit(): void {
    if (this.loading) return;

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading = true;
    this.successMessage = '';
    this.errorMessage = '';
    this.createdProblem = null;

    const payload: CreateProblemRequest = this.form.getRawValue();

    this.interviewerApi.createProblem(payload).pipe(
      finalize(() => {
        this.loading = false;
      })
    ).subscribe({
      next: problem => {
        this.createdProblem = problem;
        this.successMessage = 'Problem created successfully. You can now add test cases.';
      },
      error: err => {
        this.errorMessage = err?.error?.message ?? 'Failed to create problem.';
      },
    });
  }

  goToProblems(): void {
    this.router.navigateByUrl('/interviewer/problems');
  }

  createAnother(): void {
    this.createdProblem = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.form.reset({
      title: '',
      description: '',
      difficulty: 'Easy',
      topic: '',
      constraintsText: '',
      exampleInput: '',
      exampleOutput: '',
      executionMode: CreateProblemPage.stdinExecutionMode,
      csharpStarterCode: '',
      pythonStarterCode: '',
      cppStarterCode: '',
      csharpHarnessTemplate: '',
      pythonHarnessTemplate: '',
      cppHarnessTemplate: '',
      isPublic: true,
    });
  }

  loadTwoSumScaffold(): void {
    this.form.patchValue({
      title: this.form.controls.title.value || 'Two Sum',
      description: this.form.controls.description.value || 'Return the indices of the two numbers such that they add up to the target.',
      difficulty: this.form.controls.difficulty.value || 'Easy',
      topic: this.form.controls.topic.value || 'Arrays',
      constraintsText: this.form.controls.constraintsText.value || 'Use the hidden harness with JSON input like {"nums":[2,7,11,15],"target":9}.',
      exampleInput: this.form.controls.exampleInput.value || '{"nums":[2,7,11,15],"target":9}',
      exampleOutput: this.form.controls.exampleOutput.value || '[0,1]',
      executionMode: CreateProblemPage.functionExecutionMode,
      ...CreateProblemPage.twoSumScaffold,
    });
  }

  loadGenericFunctionScaffold(): void {
    this.form.patchValue({
      title: this.form.controls.title.value || 'Generic Function Problem',
      description: this.form.controls.description.value || 'Customize the starter signature and hidden runner for your specific problem.',
      difficulty: this.form.controls.difficulty.value || 'Easy',
      topic: this.form.controls.topic.value || 'General',
      constraintsText: this.form.controls.constraintsText.value ||
        'Generic starter template: the hidden runner passes the full raw input string into Solution.Solve(...). Replace the method name, parameters, return type, and parsing logic if you want a typed function-signature problem.',
      exampleInput: this.form.controls.exampleInput.value || 'line 1 of input\nline 2 of input',
      exampleOutput: this.form.controls.exampleOutput.value || 'single expected output value',
      executionMode: CreateProblemPage.functionExecutionMode,
      ...CreateProblemPage.genericFunctionScaffold,
    });
  }
}
