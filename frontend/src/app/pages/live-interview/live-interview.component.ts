import { Component, OnInit, ElementRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { ApiService, ChatMessage } from '../../services/api.service';

@Component({
  selector: 'app-live-interview',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './live-interview.component.html',
  styleUrls: ['./live-interview.component.css']
})
export class LiveInterviewComponent implements OnInit {
  applicationId: string = '';
  chatHistory: ChatMessage[] = [];
  newMessage: string = '';
  isTyping: boolean = false;
  error: string = '';
  isFinished: boolean = false;

  @ViewChild('chatContainer') chatContainer!: ElementRef;

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private apiService: ApiService
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const id = params.get('id');
      if (id) {
        this.applicationId = id;
        this.startInterview();
      } else {
        this.error = 'Invalid Application ID.';
      }
    });
  }

  startInterview(): void {
    this.isTyping = true;
    this.error = '';
    // Send an initial empty message to trigger the first question
    this.apiService.conductLiveInterview(this.applicationId, [], '').subscribe({
      next: (res) => {
        this.chatHistory.push({ role: 'model', text: res.reply });
        this.isTyping = false;
        this.scrollToBottom();
      },
      error: (err) => {
        this.error = 'Failed to start interview. Please try again.';
        this.isTyping = false;
        console.error(err);
      }
    });
  }

  onKeydown(event: Event): void {
    const keyboardEvent = event as KeyboardEvent;
    if (keyboardEvent.key === 'Enter' && !keyboardEvent.shiftKey) {
      keyboardEvent.preventDefault();
      this.sendMessage();
    }
  }

  sendMessage(): void {
    if (!this.newMessage.trim() || this.isTyping || this.isFinished) return;

    const userText = this.newMessage.trim();
    this.newMessage = '';
    
    // Add user message to UI
    this.chatHistory.push({ role: 'user', text: userText });
    this.scrollToBottom();
    
    this.isTyping = true;
    this.error = '';

    // Create a copy of the history to send (excluding the one we just added to the UI, 
    // because the backend expects the *previous* history and the *new* message separately)
    // Wait, the backend logic actually says:
    // It loops through chatHistory, then adds newMessage at the end.
    // So the history sent should be everything BEFORE the new user text.
    const historyToSend = this.chatHistory.slice(0, this.chatHistory.length - 1);

    this.apiService.conductLiveInterview(this.applicationId, historyToSend, userText).subscribe({
      next: (res) => {
        this.chatHistory.push({ role: 'model', text: res.reply });
        this.isTyping = false;
        this.scrollToBottom();
      },
      error: (err) => {
        this.error = 'An error occurred while sending your message.';
        this.isTyping = false;
        console.error(err);
      }
    });
  }

  finishInterview(): void {
    if (confirm('Are you sure you want to finish the interview? You can return to your dashboard.')) {
      this.isFinished = true;
      this.router.navigate(['/my-applications']);
    }
  }

  private scrollToBottom(): void {
    setTimeout(() => {
      if (this.chatContainer) {
        this.chatContainer.nativeElement.scrollTop = this.chatContainer.nativeElement.scrollHeight;
      }
    }, 50);
  }
}
