# Lint the chart (catches common mistakes)

helm lint .

# Render templates with your values

helm template image-processing . -f values.local.yaml -n demo > rendered.yaml

# Install into namespace "demo" (create it if missing)

helm install image-processing . -f values.local.yaml -n demo --create-namespace

# Normal upgrade

helm upgrade image-processing . -f values.local.yaml -n demo

# Force recreation (if patching fails due to merge errors)

helm upgrade image-processing . -f values.local.yaml -n demo --force

# Uninstall

helm uninstall image-processing -n demo

# Debug Check what’s wrong with templates:

helm upgrade image-processing . -f values.local.yaml -n demo --debug --dry-run

# Kubernetes Service Access:

# List pods and services

kubectl get pods,svc -n demo

# Port-forward service → localhost

kubectl port-forward svc/image-processing-hello-world 8080:80 -n demo

# Then open http://localhost:8080

# Logs & Status

# Check rollout

kubectl rollout status deploy/image-processing-hello-world -n demo

# View logs

kubectl logs deploy/image-processing-hello-world -n demo
