# Google Cloud Pub/Sub Infrastructure with Terraform

This Terraform configuration sets up the necessary Google Cloud infrastructure for image processing with Pub/Sub messaging.

## Resources Created

- **Pub/Sub Topic**: Main topic for image processing events
- **Pub/Sub Subscription**: Subscription to consume messages from the topic
- **Dead Letter Topic**: For handling failed message processing
- **Service Account**: With appropriate permissions for Pub/Sub operations
- **Storage Bucket**: For storing processed images (optional)
- **IAM Bindings**: Proper permissions for the service account

## Prerequisites

1. **Google Cloud Project**: You need an active GCP project
2. **Terraform**: Install Terraform >= 1.0
3. **Google Cloud CLI**: Install and authenticate with `gcloud auth login`
4. **Enable Billing**: Make sure billing is enabled on your GCP project

## Setup Instructions

1. **Clone and Navigate**:
   ```bash
   cd terraform/
   ```

2. **Configure Variables**:
   ```bash
   cp terraform.tfvars.example terraform.tfvars
   ```
   Edit `terraform.tfvars` with your project details:
   ```hcl
   project_id = "your-actual-project-id"
   region = "us-central1"
   environment = "dev"
   ```

3. **Initialize Terraform**:
   ```bash
   terraform init
   ```

4. **Plan the Deployment**:
   ```bash
   terraform plan
   ```

5. **Apply the Configuration**:
   ```bash
   terraform apply
   ```

6. **Get Service Account Key**:
   After deployment, get the service account key:
   ```bash
   terraform output -raw service_account_key > ../service-account-key.json
   ```

## Environment Variables for Your Application

After deployment, set these environment variables in your .NET application:

```bash
export GOOGLE_APPLICATION_CREDENTIALS="./service-account-key.json"
export GOOGLE_CLOUD_PROJECT="your-project-id"
export PUBSUB_TOPIC="image-processing-events"
export PUBSUB_SUBSCRIPTION="image-processing-events-subscription"
```

## Cleanup

To destroy all resources:
```bash
terraform destroy
```

## Security Notes

- Service account keys are sensitive - never commit them to version control
- The `.gitignore` file is configured to exclude sensitive files
- Consider using Workload Identity for production deployments instead of service account keys
